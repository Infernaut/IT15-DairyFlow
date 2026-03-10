using IT15_DairyFlow.Models.Admin;
using IT15_DairyFlow.Models;
using IT15_DairyFlow.Data;
using IT15_DairyFlow.Services;
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
        private readonly NotificationService _notificationService;

        public SuperAdminController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            NotificationService notificationService)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _notificationService = notificationService;
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

            // Annual Recurring Revenue
            var arr = mrr * 12;

            // Invoice-based revenue
            var invoices = await _dbContext.BillingInvoice.ToListAsync();
            var paidInvoices = invoices.Where(i => i.PaymentStatus == "Paid" && i.InvoiceDate.HasValue).ToList();
            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;
            var monthlyInvoiceRevenue = paidInvoices
                .Where(i => i.InvoiceDate!.Value.Month == currentMonth && i.InvoiceDate!.Value.Year == currentYear)
                .Sum(i => i.Amount ?? 0);
            var annualInvoiceRevenue = paidInvoices
                .Where(i => i.InvoiceDate!.Value.Year == currentYear)
                .Sum(i => i.Amount ?? 0);

            // Monthly revenue trend (last 12 months)
            var monthlyLabels = new List<string>();
            var monthlyData = new List<decimal>();
            for (int i = 11; i >= 0; i--)
            {
                var date = DateTime.UtcNow.AddMonths(-i);
                monthlyLabels.Add(date.ToString("MMM yyyy"));
                var monthRevenue = paidInvoices
                    .Where(inv => inv.InvoiceDate!.Value.Month == date.Month && inv.InvoiceDate!.Value.Year == date.Year)
                    .Sum(inv => inv.Amount ?? 0);
                monthlyData.Add(monthRevenue);
            }

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
                AnnualRecurringRevenue = arr,
                MonthlyInvoiceRevenue = monthlyInvoiceRevenue,
                AnnualInvoiceRevenue = annualInvoiceRevenue,
                MonthlyRevenueLabels = monthlyLabels,
                MonthlyRevenueData = monthlyData,
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
                int? remainingDays = CalculateRemainingDays(company);

                companyViewModels.Add(new CompanyListItemViewModel
                {
                    CompanyID = company.CompanyID,
                    CompanyName = company.CompanyName,
                    Status = company.Status ?? "Unknown",
                    SubscriptionPlan = company.Subscription?.PlanName ?? "None",
                    BillingCycle = company.Subscription?.BillingCycle ?? "-",
                    Price = company.Subscription?.Price ?? 0,
                    UserCount = userCount,
                    RemainingDays = remainingDays,
                    SubscriptionStartDate = company.SubscriptionStartDate
                });
            }

            var subscriptions = await _dbContext.Subscription.OrderBy(s => s.PlanName).ToListAsync();
            var model = new CompaniesPageViewModel
            {
                Companies = companyViewModels,
                AddCompany = new AddCompanyViewModel(),
                SubscriptionOptions = subscriptions.Select(s => new SubscriptionOptionItem
                {
                    SubscriptionID = s.SubscriptionID,
                    DisplayName = $"{s.PlanName} — ₱{s.Price:N0}/{s.BillingCycle}"
                }).ToList()
            };

            return View(model);
        }

        // ─── ADD COMPANY (manual) ─────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCompany(CompaniesPageViewModel form)
        {
            var addModel = form.AddCompany;

            // Re-validate only the AddCompany sub-model
            if (string.IsNullOrWhiteSpace(addModel.CompanyName) ||
                string.IsNullOrWhiteSpace(addModel.AdminEmail) ||
                string.IsNullOrWhiteSpace(addModel.AdminUserName) ||
                string.IsNullOrWhiteSpace(addModel.Password))
            {
                TempData["ErrorMessage"] = "All fields are required to add a company.";
                return RedirectToAction(nameof(Companies));
            }

            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(addModel.AdminEmail);
            if (existingUser != null)
            {
                TempData["ErrorMessage"] = "A user with that email already exists.";
                return RedirectToAction(nameof(Companies));
            }

            // Create Company
            var company = new Company
            {
                CompanyName = addModel.CompanyName,
                SubscriptionID = addModel.SubscriptionID,
                Status = addModel.Status ?? "Active",
                SubscriptionStartDate = addModel.SubscriptionID.HasValue ? DateTime.UtcNow : null
            };

            _dbContext.Company.Add(company);
            await _dbContext.SaveChangesAsync();

            // Create Admin user
            var user = new ApplicationUser
            {
                UserName = addModel.AdminUserName,
                Email = addModel.AdminEmail,
                EmailConfirmed = true,
                CompanyID = company.CompanyID
            };

            var result = await _userManager.CreateAsync(user, addModel.Password);
            if (!result.Succeeded)
            {
                // Rollback company
                _dbContext.Company.Remove(company);
                await _dbContext.SaveChangesAsync();

                var errors = string.Join(" ", result.Errors.Select(e => e.Description));
                TempData["ErrorMessage"] = $"Failed to create admin user: {errors}";
                return RedirectToAction(nameof(Companies));
            }

            await _userManager.AddToRoleAsync(user, "Admin");
            await LogAuditAsync($"Manually added company: {company.CompanyName} with admin {addModel.AdminEmail}");
            await _notificationService.NotifySuperAdminsAsync(
                $"New company added: {company.CompanyName}", "System", "bi-building-add",
                _userManager.GetUserId(User));

            TempData["SuccessMessage"] = $"Company \"{company.CompanyName}\" created successfully with admin {addModel.AdminEmail}.";
            return RedirectToAction(nameof(Companies));
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
            await _notificationService.NotifySuperAdminsAsync(
                $"Company status changed: {company.CompanyName} → {company.Status}", "System", "bi-toggle-on",
                _userManager.GetUserId(User));

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

        /// <summary>
        /// Calculates remaining subscription days based on start date and billing cycle.
        /// Free Trial = 14 days, Monthly = 30 days, Annual = 365 days.
        /// </summary>
        private static int? CalculateRemainingDays(Company company)
        {
            if (company.SubscriptionStartDate == null || company.Subscription == null)
                return null;

            var cycle = company.Subscription.BillingCycle?.ToLower() ?? "";
            int totalDays = cycle switch
            {
                "14 days" => 14,
                "monthly" => 30,
                "annual" => 365,
                _ => 30
            };

            var elapsed = (DateTime.UtcNow - company.SubscriptionStartDate.Value).TotalDays;
            var remaining = totalDays - (int)elapsed;
            return remaining < 0 ? 0 : remaining;
        }

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
