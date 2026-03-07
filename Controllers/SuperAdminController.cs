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
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _dbContext;

        public SuperAdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _roleManager = roleManager;
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
            var admins = 0;
            foreach (var u in allUsers)
            {
                if (await _userManager.IsInRoleAsync(u, "Admin")) admins++;
            }

            var totalProducts = await _dbContext.Product.CountAsync();
            var totalBatches = await _dbContext.ProductionBatch.CountAsync();
            var completedBatches = await _dbContext.ProductionBatch.CountAsync(b => b.Status == "Completed");
            var totalInspections = await _dbContext.QualityInspection.CountAsync();
            var passedInspections = await _dbContext.QualityInspection.CountAsync(q => q.Result == "pass");
            var failedInspections = await _dbContext.QualityInspection.CountAsync(q => q.Result == "fail");
            var totalExpenses = await _dbContext.Expense.SumAsync(e => (decimal?)e.Amount ?? 0);
            var totalRevenue = await _dbContext.BillingInvoice
                .Where(b => b.PaymentStatus == "Paid")
                .SumAsync(b => (decimal?)b.Amount ?? 0);
            var totalInventory = await _dbContext.Inventory.CountAsync();

            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
            var monthlyBatches = await _dbContext.ProductionBatch
                .Where(b => b.StartDate >= sixMonthsAgo)
                .GroupBy(b => new { b.StartDate.Value.Year, b.StartDate.Value.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToListAsync();

            var recentLogs = await _dbContext.AuditLog
                .Include(a => a.User)
                .Include(a => a.Company)
                .OrderByDescending(a => a.TimeStamp)
                .Take(20)
                .ToListAsync();

            var model = new SuperAdminDashboardViewModel
            {
                TotalCompanies = totalCompanies,
                ActiveCompanies = activeCompanies,
                TrialCompanies = trialCompanies,
                ActiveSubscriptions = activeSubscriptions,
                MonthlyRecurringRevenue = mrr,
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                AdminUsers = admins,
                TotalProducts = totalProducts,
                TotalBatches = totalBatches,
                CompletedBatches = completedBatches,
                TotalInspections = totalInspections,
                PassedInspections = passedInspections,
                FailedInspections = failedInspections,
                TotalExpenses = totalExpenses,
                TotalRevenue = totalRevenue,
                TotalInventoryItems = totalInventory,
                MonthlyBatchLabels = monthlyBatches.Select(m => $"{m.Year}-{m.Month:D2}").ToList(),
                MonthlyBatchCounts = monthlyBatches.Select(m => m.Count).ToList(),
                RecentAuditLogs = recentLogs.Select(log => new SuperAdminAuditLogViewModel
                {
                    AuditLogID = log.AuditLogID,
                    UserEmail = log.User?.Email ?? "Unknown",
                    UserName = log.User?.UserName ?? "Unknown",
                    CompanyName = log.Company?.CompanyName ?? "Unknown",
                    Action = log.Action ?? "Unknown",
                    TimeStamp = log.TimeStamp
                }).ToList()
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

            var companyStats = new List<CompanyAnalyticsViewModel>();
            foreach (var company in companies)
            {
                var cid = company.CompanyID;
                companyStats.Add(new CompanyAnalyticsViewModel
                {
                    CompanyName = company.CompanyName,
                    UserCount = await _userManager.Users.CountAsync(u => u.CompanyID == cid),
                    ProductCount = await _dbContext.Product.CountAsync(p => p.CompanyID == cid),
                    BatchCount = await _dbContext.ProductionBatch.CountAsync(b => b.CompanyID == cid),
                    InspectionCount = await _dbContext.QualityInspection.CountAsync(q => q.CompanyID == cid),
                    InventoryCount = await _dbContext.Inventory.CountAsync(i => i.CompanyID == cid),
                    TotalExpenses = await _dbContext.Expense.Where(e => e.CompanyID == cid).SumAsync(e => (decimal?)e.Amount ?? 0),
                    TotalRevenue = await _dbContext.BillingInvoice.Where(b => b.CompanyID == cid && b.PaymentStatus == "Paid").SumAsync(b => (decimal?)b.Amount ?? 0)
                });
            }

            var model = new AnalyticsPageViewModel
            {
                CompanyStats = companyStats,
                CompanyLabels = companyStats.Select(c => c.CompanyName).ToList(),
                CompanyUserCounts = companyStats.Select(c => c.UserCount).ToList(),
                CompanyBatchCounts = companyStats.Select(c => c.BatchCount).ToList(),
                CompanyRevenues = companyStats.Select(c => c.TotalRevenue).ToList()
            };

            return View(model);
        }

        // ─── LOGS ─────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Logs(DateTime? startDate = null, DateTime? endDate = null, string? actionFilter = null)
        {
            var query = _dbContext.AuditLog
                .Include(a => a.User)
                .Include(a => a.Company)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(a => a.TimeStamp >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(a => a.TimeStamp <= endDate.Value.AddDays(1));
            if (!string.IsNullOrWhiteSpace(actionFilter))
                query = query.Where(a => a.Action!.Contains(actionFilter));

            var logs = await query.OrderByDescending(a => a.TimeStamp).ToListAsync();

            var model = new SuperAdminLogFilterViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                ActionFilter = actionFilter,
                AuditLogs = logs.Select(log => new SuperAdminAuditLogViewModel
                {
                    AuditLogID = log.AuditLogID,
                    UserEmail = log.User?.Email ?? "Unknown",
                    UserName = log.User?.UserName ?? "Unknown",
                    CompanyName = log.Company?.CompanyName ?? "Unknown",
                    Action = log.Action ?? "Unknown",
                    TimeStamp = log.TimeStamp
                }).ToList(),
                TotalRecords = logs.Count
            };

            return View(model);
        }

        // ─── USER MANAGEMENT (ALL COMPANIES) ─────────────────────
        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();

            var items = new List<SuperAdminUserListItemViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var company = user.CompanyID.HasValue
                    ? await _dbContext.Company.FindAsync(user.CompanyID.Value)
                    : null;

                items.Add(new SuperAdminUserListItemViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    Roles = roles.Count > 0 ? string.Join(", ", roles) : "None",
                    IsActive = IsUserActive(user),
                    CompanyName = company?.CompanyName ?? "No Company",
                    CompanyID = user.CompanyID
                });
            }

            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Superadmin")) return RedirectToAction(nameof(Users));

            var isActive = IsUserActive(user);
            user.LockoutEnabled = true;
            user.LockoutEnd = isActive ? DateTimeOffset.MaxValue : null;

            await _userManager.UpdateAsync(user);
            await LogAuditAsync($"Toggled user status: {user.Email}");

            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        public async Task<IActionResult> UserRoles(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();

            var model = new UserRolesViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? user.UserName ?? "User",
                Roles = new List<UserRoleItemViewModel>()
            };

            foreach (var role in roles)
            {
                var roleName = role.Name ?? string.Empty;
                model.Roles.Add(new UserRoleItemViewModel
                {
                    RoleId = role.Id,
                    RoleName = roleName,
                    Selected = !string.IsNullOrEmpty(roleName) && await _userManager.IsInRoleAsync(user, roleName)
                });
            }

            return View("~/Views/Admin/UserRoles.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserRoles(UserRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            var selectedRoles = model.Roles
                .Where(r => r.Selected)
                .Select(r => r.RoleName)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .ToList();

            var currentRoles = await _userManager.GetRolesAsync(user);
            var toRemove = currentRoles.Except(selectedRoles).ToList();
            var toAdd = selectedRoles.Except(currentRoles).ToList();

            if (toRemove.Count > 0)
            {
                await _userManager.RemoveFromRolesAsync(user, toRemove);
                await LogAuditAsync($"Removed roles from {user.Email}: {string.Join(", ", toRemove)}");
            }
            if (toAdd.Count > 0)
            {
                await _userManager.AddToRolesAsync(user, toAdd);
                await LogAuditAsync($"Added roles to {user.Email}: {string.Join(", ", toAdd)}");
            }

            return RedirectToAction(nameof(Users));
        }

        // ─── ROLE MANAGEMENT ──────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Roles()
        {
            var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            var items = roles.Select(r => new RoleListItemViewModel
            {
                Id = r.Id,
                Name = r.Name ?? string.Empty
            }).ToList();

            return View("~/Views/Admin/Roles.cshtml", items);
        }

        [HttpGet]
        public IActionResult CreateRole()
        {
            return View("~/Views/Admin/CreateRole.cshtml", new RoleEditViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(RoleEditViewModel model)
        {
            if (!ModelState.IsValid) return View("~/Views/Admin/CreateRole.cshtml", model);

            if (await _roleManager.RoleExistsAsync(model.Name))
            {
                ModelState.AddModelError(nameof(model.Name), "Role already exists.");
                return View("~/Views/Admin/CreateRole.cshtml", model);
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(model.Name));
            if (result.Succeeded)
            {
                await LogAuditAsync($"Created role: {model.Name}");
                return RedirectToAction(nameof(Roles));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View("~/Views/Admin/CreateRole.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> EditRole(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            return View("~/Views/Admin/EditRole.cshtml", new RoleEditViewModel
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(RoleEditViewModel model)
        {
            if (!ModelState.IsValid) return View("~/Views/Admin/EditRole.cshtml", model);

            var role = await _roleManager.FindByIdAsync(model.Id);
            if (role == null) return NotFound();

            role.Name = model.Name;
            var result = await _roleManager.UpdateAsync(role);
            if (result.Succeeded)
            {
                await LogAuditAsync($"Updated role: {model.Name}");
                return RedirectToAction(nameof(Roles));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View("~/Views/Admin/EditRole.cshtml", model);
        }

        // ─── HELPERS ──────────────────────────────────────────────
        private static bool IsUserActive(ApplicationUser user)
        {
            if (!user.LockoutEnd.HasValue) return true;
            return user.LockoutEnd <= DateTimeOffset.UtcNow;
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
