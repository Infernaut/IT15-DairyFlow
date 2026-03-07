using IT15_DairyFlow.Models.Admin;
using IT15_DairyFlow.Models;
using IT15_DairyFlow.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IT15_DairyFlow.Controllers
{
    [Authorize(Roles = "Superadmin")]
    public class SuperAdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;

        public SuperAdminController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;
        }

        // ─── DASHBOARD ────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var companies = await _dbContext.Company.Include(c => c.Subscription).ToListAsync();
            var allUsers = await _userManager.Users.ToListAsync();
            var now = DateTimeOffset.UtcNow;

            var totalCompanies = companies.Count;
            var activeCompanies = companies.Count(c => c.Status == "Active");
            var trialCompanies = companies.Count(c => c.Status == "Trial");
            var activeSubscriptions = companies.Count(c => c.SubscriptionID != null);
            var mrr = companies
                .Where(c => c.Subscription != null)
                .Sum(c => c.Subscription!.Price ?? 0);

            var totalUsers = allUsers.Count;
            var activeUsers = allUsers.Count(u => !u.LockoutEnd.HasValue || u.LockoutEnd <= now);

            // Per-company user counts for chart
            var companyUserCounts = new List<CompanyAnalyticsViewModel>();
            foreach (var company in companies.OrderBy(c => c.CompanyName))
            {
                var cid = company.CompanyID;
                companyUserCounts.Add(new CompanyAnalyticsViewModel
                {
                    CompanyName = company.CompanyName,
                    UserCount = allUsers.Count(u => u.CompanyID == cid),
                    SubscriptionPlan = company.Subscription?.PlanName ?? "None",
                    Status = company.Status ?? "Unknown"
                });
            }

            var model = new SuperAdminDashboardViewModel
            {
                TotalCompanies = totalCompanies,
                ActiveCompanies = activeCompanies,
                TrialCompanies = trialCompanies,
                ActiveSubscriptions = activeSubscriptions,
                MonthlyRecurringRevenue = mrr,
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                CompanyUserStats = companyUserCounts
            };

            return View(model);
        }

        // ─── COMPANIES ────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Companies()
        {
            var companies = await _dbContext.Company
                .Include(c => c.Subscription)
                .OrderBy(c => c.CompanyName)
                .ToListAsync();

            var companyViewModels = new List<CompanyListItemViewModel>();
            foreach (var company in companies)
            {
                var userCount = await _userManager.Users.CountAsync(u => u.CompanyID == company.CompanyID);
                companyViewModels.Add(new CompanyListItemViewModel
                {
                    CompanyID = company.CompanyID,
                    CompanyName = company.CompanyName,
                    Status = company.Status ?? "Unknown",
                    SubscriptionPlan = company.Subscription?.PlanName ?? "None",
                    BillingCycle = company.Subscription?.BillingCycle ?? "-",
                    Price = company.Subscription?.Price ?? 0,
                    UserCount = userCount
                });
            }

            return View(companyViewModels);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCompanyStatus(int id)
        {
            var company = await _dbContext.Company.FindAsync(id);
            if (company == null) return NotFound();

            company.Status = company.Status == "Active" ? "Inactive" : "Active";
            _dbContext.Company.Update(company);
            await _dbContext.SaveChangesAsync();
            await LogAuditAsync($"Toggled company status: {company.CompanyName} → {company.Status}");

            return RedirectToAction(nameof(Companies));
        }

        // ─── SUBSCRIPTIONS ────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Subscriptions()
        {
            var subscriptions = await _dbContext.Subscription
                .Include(s => s.Companies)
                .OrderBy(s => s.PlanName)
                .ToListAsync();

            var model = subscriptions.Select(s => new SubscriptionListItemViewModel
            {
                SubscriptionID = s.SubscriptionID,
                PlanName = s.PlanName ?? "Unnamed",
                Price = s.Price ?? 0,
                BillingCycle = s.BillingCycle ?? "-",
                CompanyCount = s.Companies.Count
            }).ToList();

            return View(model);
        }

        // ─── ANALYTICS ────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Analytics()
        {
            var companies = await _dbContext.Company.Include(c => c.Subscription).ToListAsync();
            var allUsers = await _userManager.Users.ToListAsync();

            var companyStats = new List<CompanyAnalyticsViewModel>();
            foreach (var company in companies.OrderBy(c => c.CompanyName))
            {
                var cid = company.CompanyID;
                companyStats.Add(new CompanyAnalyticsViewModel
                {
                    CompanyName = company.CompanyName,
                    UserCount = allUsers.Count(u => u.CompanyID == cid),
                    SubscriptionPlan = company.Subscription?.PlanName ?? "None",
                    Status = company.Status ?? "Unknown"
                });
            }

            var model = new AnalyticsPageViewModel
            {
                CompanyStats = companyStats,
                CompanyLabels = companyStats.Select(c => c.CompanyName).ToList(),
                CompanyUserCounts = companyStats.Select(c => c.UserCount).ToList()
            };

            return View(model);
        }

        // ─── HELPERS ──────────────────────────────────────────────
        private async Task LogAuditAsync(string action)
        {
            var currentUserId = _userManager.GetUserId(User) ?? string.Empty;
            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            if (currentUser?.CompanyID == null) return;

            _dbContext.AuditLog.Add(new AuditLog
            {
                CompanyID = currentUser.CompanyID.Value,
                UserID = currentUserId,
                Action = action,
                TimeStamp = DateTime.Now
            });
            await _dbContext.SaveChangesAsync();
        }
    }
}
