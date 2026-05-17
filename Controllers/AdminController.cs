using IT15_DairyFlow.Models.Admin;
using IT15_DairyFlow.Models;
using IT15_DairyFlow.Data;
using IT15_DairyFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IT15_DairyFlow.Security.Crypto;

namespace IT15_DairyFlow.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private const string SuperAdminRoleName = "Superadmin";
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly NotificationService _notificationService;
    private readonly ICryptoService _crypto;

        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext dbContext, NotificationService notificationService, ICryptoService crypto)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _dbContext = dbContext;
            _notificationService = notificationService;
            _crypto = crypto;
        }

        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var currentUserId = _userManager.GetUserId(User) ?? string.Empty;
            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            var currentUserCompanyId = currentUser?.CompanyID;

            // Only show users with the same CompanyID as the current admin, excluding the current admin
            var users = await _userManager.Users
                .Where(u => u.CompanyID == currentUserCompanyId && u.Id != currentUserId)
                .OrderBy(u => u.Email)
                .ToListAsync();

            var items = new List<UserListItemViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var isProtected = roles.Contains(SuperAdminRoleName);
                items.Add(new UserListItemViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    Roles = roles.Count > 0 ? string.Join(", ", roles) : "None",
                    IsActive = IsUserActive(user),
                    IsProtected = isProtected
                });
            }

            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var currentUserId = _userManager.GetUserId(User) ?? string.Empty;
            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                IsActive = IsUserActive(user),
                IsProtected = user.Id == currentUserId || roles.Contains(SuperAdminRoleName),
                RoleSummary = roles.Count > 0 ? string.Join(", ", roles) : "None"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var currentUserId = _userManager.GetUserId(User) ?? string.Empty;
            var isProtected = user.Id == currentUserId || roles.Contains(SuperAdminRoleName);

            UserEmailProtector.ProtectEmail(user, _crypto, model.Email);
            user.UserName = model.UserName;
            user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber;

            if (!isProtected)
            {
                user.LockoutEnabled = true;
                user.LockoutEnd = model.IsActive ? null : DateTimeOffset.MaxValue;
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                model.IsProtected = isProtected;
                model.RoleSummary = roles.Count > 0 ? string.Join(", ", roles) : "None";
                return View(model);
            }

            await LogAuditAsync($"Updated user: {user.Email}");
            await NotifyAdminActionAsync($"Updated user: {user.Email}", "Admin", "bi-person-gear");
            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            return View(new CreateUserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Prevent duplicate emails (email stored encrypted; use deterministic lookup hash)
            var emailLookup = _crypto.ComputeLookupHash(model.Email);
            var existing = await _userManager.Users.FirstOrDefaultAsync(u => u.EmailLookupHash == emailLookup);
            if (existing != null)
            {
                ModelState.AddModelError(nameof(model.Email), "A user with that email already exists.");
                return View(model);
            }

            // Get the current admin's CompanyID
            var currentUserId = _userManager.GetUserId(User) ?? string.Empty;
            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            var companyId = currentUser?.CompanyID;

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                EmailConfirmed = true,
                CompanyID = companyId  // Set to admin's CompanyID
            };

            UserEmailProtector.ProtectEmail(user, _crypto, model.Email);

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await LogAuditAsync($"Created user: {user.Email}");
                await NotifyAdminActionAsync($"Created new user: {user.Email}", "Admin", "bi-person-plus");
                return RedirectToAction(nameof(Users));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var currentUserId = _userManager.GetUserId(User) ?? string.Empty;
            if (user.Id == currentUserId || roles.Contains(SuperAdminRoleName))
            {
                return RedirectToAction(nameof(Users));
            }

            var isActive = IsUserActive(user);
            user.LockoutEnabled = true;
            user.LockoutEnd = isActive ? DateTimeOffset.MaxValue : null;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            else
            {
                await LogAuditAsync($"Toggled user status: {user.Email}");
                await NotifyAdminActionAsync($"Toggled user status: {user.Email}", "Admin", "bi-person-check");
            }

            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        public async Task<IActionResult> Roles()
        {
            var roles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .ToListAsync();

            var items = roles.Select(role => new RoleListItemViewModel
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty
            }).ToList();

            return View(items);
        }

        [HttpGet]
        public IActionResult CreateRole()
        {
            return View(new RoleEditViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(RoleEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _roleManager.RoleExistsAsync(model.Name))
            {
                ModelState.AddModelError(nameof(model.Name), "Role already exists.");
                return View(model);
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(model.Name));
            if (result.Succeeded)
            {
                await LogAuditAsync($"Created role: {model.Name}");
                return RedirectToAction(nameof(Roles));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditRole(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            return View(new RoleEditViewModel
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(RoleEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var role = await _roleManager.FindByIdAsync(model.Id);
            if (role == null)
            {
                return NotFound();
            }

            role.Name = model.Name;
            var result = await _roleManager.UpdateAsync(role);
            if (result.Succeeded)
            {
                await LogAuditAsync($"Updated role: {model.Name}");
                return RedirectToAction(nameof(Roles));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> UserRoles(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .ToListAsync();

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

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var currentUserId = _userManager.GetUserId(User) ?? string.Empty;
            var currentUser = await _userManager.FindByIdAsync(currentUserId);

            if (currentUser?.CompanyID == null)
            {
                return BadRequest("User does not have a company assigned.");
            }

            var companyId = currentUser.CompanyID.Value;
            var company = await _dbContext.Company.FindAsync(companyId);

            // Get KPI Metrics
            var kpiMetrics = await GetKPIMetricsAsync(companyId);

            // Get recent audit logs (last 20)
            var auditLogs = await _dbContext.AuditLog
                .Where(a => a.CompanyID == companyId)
                .OrderByDescending(a => a.TimeStampUtc)
                .Take(20)
                .Include(a => a.User)
                .ToListAsync();

            var auditLogViewModels = auditLogs.Select(log => new AuditLogViewModel
            {
                AuditLogID = log.AuditLogID,
                UserName = log.User?.UserName ?? log.User?.Email ?? "Unknown",
                Action = string.IsNullOrWhiteSpace(log.Message) ? (log.ActionType ?? "Unknown") : log.Message,
                TimeStamp = log.TimeStampUtc.UtcDateTime
            }).ToList();

            // Get user counts with EF-translatable expressions
            var now = DateTimeOffset.UtcNow;
            var totalUsers = await _userManager.Users.Where(u => u.CompanyID == companyId).CountAsync();
            var activeUsers = await _userManager.Users.Where(u => u.CompanyID == companyId && (!u.LockoutEnd.HasValue || u.LockoutEnd <= now)).CountAsync();
            var inactiveUsers = await _userManager.Users.Where(u => u.CompanyID == companyId && u.LockoutEnd.HasValue && u.LockoutEnd > now).CountAsync();

            // Build dashboard view model
            var model = new AdminDashboardViewModel
            {
                CompanyName = company?.CompanyName ?? "Unknown Company",
                CompanyID = companyId,
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                InactiveUsers = inactiveUsers,
                AdminUsers = 0, // Will be calculated from roles
                KPIMetrics = kpiMetrics,
                RecentAuditLogs = auditLogViewModels
            };

            // Get admin count
            var allUsersInCompany = await _userManager.Users.Where(u => u.CompanyID == companyId).ToListAsync();
            var adminCount = 0;
            foreach (var user in allUsersInCompany)
            {
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    adminCount++;
                }
            }
            model.AdminUsers = adminCount;

            // ── Chart data: Monthly production batches (last 6 months) ──
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
            var monthlyBatches = await _dbContext.ProductionBatch
                .Where(b => b.CompanyID == companyId && b.StartDate >= sixMonthsAgo)
                .GroupBy(b => new { b.StartDate.Value.Year, b.StartDate.Value.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToListAsync();

            model.MonthlyBatchLabels = monthlyBatches.Select(m => $"{m.Year}-{m.Month:D2}").ToList();
            model.MonthlyBatchCounts = monthlyBatches.Select(m => m.Count).ToList();

            // ── Chart data: Monthly revenue vs expenses (last 6 months) ──
            var monthlyRevenue = await _dbContext.BillingInvoice
                .Where(b => b.CompanyID == companyId && b.PaymentStatus == "Paid" && b.InvoiceDate >= sixMonthsAgo)
                .GroupBy(b => new { b.InvoiceDate.Value.Year, b.InvoiceDate.Value.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(b => (decimal?)b.Amount ?? 0) })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToListAsync();

            var monthlyExpenses = await _dbContext.Expense
                .Where(e => e.CompanyID == companyId && e.ExpenseDate >= sixMonthsAgo)
                .GroupBy(e => new { e.ExpenseDate.Value.Year, e.ExpenseDate.Value.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(e => (decimal?)e.Amount ?? 0) })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToListAsync();

            // Build union of all months
            var allMonthKeys = monthlyRevenue.Select(r => $"{r.Year}-{r.Month:D2}")
                .Union(monthlyExpenses.Select(e => $"{e.Year}-{e.Month:D2}"))
                .OrderBy(x => x).Distinct().ToList();

            model.MonthlyRevenueLabels = allMonthKeys;
            model.MonthlyRevenues = allMonthKeys.Select(k =>
            {
                var parts = k.Split('-');
                var yr = int.Parse(parts[0]); var mo = int.Parse(parts[1]);
                return monthlyRevenue.FirstOrDefault(r => r.Year == yr && r.Month == mo)?.Total ?? 0;
            }).ToList();
            model.MonthlyExpenses = allMonthKeys.Select(k =>
            {
                var parts = k.Split('-');
                var yr = int.Parse(parts[0]); var mo = int.Parse(parts[1]);
                return monthlyExpenses.FirstOrDefault(e => e.Year == yr && e.Month == mo)?.Total ?? 0;
            }).ToList();

            return View(model);
        }

        private async Task<KPIMetricsViewModel> GetKPIMetricsAsync(int companyId)
        {
            var kpiMetrics = new KPIMetricsViewModel();

            // User Management KPIs
            var allUsersInCompany = await _userManager.Users.Where(u => u.CompanyID == companyId).ToListAsync();
            var activeUsers = allUsersInCompany.Where(u => IsUserActive(u)).Count();
            var inactiveUsers = allUsersInCompany.Where(u => !IsUserActive(u)).Count();

            kpiMetrics.TotalActiveUsers = activeUsers;
            kpiMetrics.TotalInactiveUsers = inactiveUsers;
            if (allUsersInCompany.Count > 0)
            {
                kpiMetrics.UserGrowthPercentage = (decimal)activeUsers / allUsersInCompany.Count * 100;
            }

            // Product Management KPIs
            kpiMetrics.TotalProducts = await _dbContext.Product.Where(p => p.CompanyID == companyId).CountAsync();
            kpiMetrics.ActiveProducts = await _dbContext.Product.Where(p => p.CompanyID == companyId && p.LifecycleStatus == "Active").CountAsync();
            kpiMetrics.DiscontinuedProducts = await _dbContext.Product.Where(p => p.CompanyID == companyId && p.LifecycleStatus == "Discontinued").CountAsync();

            // Production KPIs
            kpiMetrics.TotalProductionBatches = await _dbContext.ProductionBatch.Where(p => p.CompanyID == companyId).CountAsync();
            kpiMetrics.CompletedBatches = await _dbContext.ProductionBatch.Where(p => p.CompanyID == companyId && p.Status == "Completed").CountAsync();
            kpiMetrics.OngoingBatches = await _dbContext.ProductionBatch.Where(p => p.CompanyID == companyId && p.Status == "In Production").CountAsync();
            if (kpiMetrics.TotalProductionBatches > 0)
            {
                kpiMetrics.ProductionCompletionRate = (decimal)kpiMetrics.CompletedBatches / kpiMetrics.TotalProductionBatches * 100;
            }

            // Quality Inspection KPIs
            kpiMetrics.TotalQualityInspections = await _dbContext.QualityInspection.Where(q => q.CompanyID == companyId).CountAsync();
            kpiMetrics.PassedInspections = await _dbContext.QualityInspection.Where(q => q.CompanyID == companyId && q.Result == "pass").CountAsync();
            kpiMetrics.FailedInspections = await _dbContext.QualityInspection.Where(q => q.CompanyID == companyId && q.Result == "fail").CountAsync();
            if (kpiMetrics.TotalQualityInspections > 0)
            {
                kpiMetrics.QualityPassRate = (decimal)kpiMetrics.PassedInspections / kpiMetrics.TotalQualityInspections * 100;
            }

            // Inventory KPIs
            kpiMetrics.TotalInventoryItems = await _dbContext.Inventory.Where(i => i.CompanyID == companyId).CountAsync();
            // Assuming there's a threshold for low stock (e.g., < 10 units)
            var inventoryItems = await _dbContext.Inventory.Where(i => i.CompanyID == companyId).ToListAsync();
            kpiMetrics.LowStockItems = inventoryItems.Count(i => (i.Quantity ?? 0) < 10);
            if (inventoryItems.Count > 0)
            {
                var totalQuantity = inventoryItems.Sum(i => i.Quantity ?? 0);
                kpiMetrics.AverageInventoryHealth = (decimal)(totalQuantity / inventoryItems.Count);
            }

            // Financial KPIs
            kpiMetrics.TotalExpenses = await _dbContext.Expense.Where(e => e.CompanyID == companyId).SumAsync(e => (decimal?)e.Amount ?? 0);

            var totalBudget = await _dbContext.Budget.Where(b => b.CompanyID == companyId).SumAsync(b => (decimal?)b.AllocatedAmount ?? 0);
            if (totalBudget > 0)
            {
                kpiMetrics.BudgetUtilization = (kpiMetrics.TotalExpenses / totalBudget) * 100;
            }

            var totalInvoicesAmount = await _dbContext.BillingInvoice
                .Where(b => b.CompanyID == companyId && b.PaymentStatus == "Paid")
                .SumAsync(b => (decimal?)b.Amount ?? 0);
            kpiMetrics.TotalRevenue = totalInvoicesAmount;

            // System KPIs
            var lastAuditLog = await _dbContext.AuditLog
                .Where(a => a.CompanyID == companyId)
                .OrderByDescending(a => a.TimeStampUtc)
                .FirstOrDefaultAsync();
            kpiMetrics.LastModifiedDate = lastAuditLog?.TimeStampUtc.UtcDateTime ?? DateTime.UtcNow;

            kpiMetrics.SystemActivityCount = await _dbContext.AuditLog
                .Where(a => a.CompanyID == companyId && a.TimeStampUtc >= DateTimeOffset.UtcNow.AddDays(-30))
                .CountAsync();

            return kpiMetrics;
        }

        [HttpGet]
        public async Task<IActionResult> Logs(DateTime? startDate = null, DateTime? endDate = null, string? actionFilter = null)
        {
            var currentUserId = _userManager.GetUserId(User) ?? string.Empty;
            var currentUser = await _userManager.FindByIdAsync(currentUserId);

            if (currentUser?.CompanyID == null)
            {
                return BadRequest("User does not have a company assigned.");
            }

            var companyId = currentUser.CompanyID.Value;

            var query = _dbContext.AuditLog
                .Where(a => a.CompanyID == companyId)
                .Include(a => a.User)
                .AsQueryable();

            // Apply filters
            if (startDate.HasValue)
            {
                query = query.Where(a => a.TimeStampUtc >= new DateTimeOffset(startDate.Value, TimeSpan.Zero));
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.TimeStampUtc <= new DateTimeOffset(endDate.Value.AddDays(1), TimeSpan.Zero));
            }

            if (!string.IsNullOrWhiteSpace(actionFilter))
            {
                query = query.Where(a => (a.Message ?? a.ActionType).Contains(actionFilter));
            }

            var auditLogs = await query
                .OrderByDescending(a => a.TimeStampUtc)
                .ToListAsync();

            var auditLogViewModels = auditLogs.Select(log => new AuditLogViewModel
            {
                AuditLogID = log.AuditLogID,
                UserName = log.User?.UserName ?? log.User?.Email ?? "Unknown",
                Action = string.IsNullOrWhiteSpace(log.Message) ? (log.ActionType ?? "Unknown") : log.Message,
                TimeStamp = log.TimeStampUtc.UtcDateTime
            }).ToList();

            var model = new AuditLogFilterViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                ActionFilter = actionFilter,
                AuditLogs = auditLogViewModels,
                TotalRecords = auditLogViewModels.Count
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserRoles(UserRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            var selectedRoles = model.Roles
                .Where(role => role.Selected)
                .Select(role => role.RoleName)
                .Where(roleName => !string.IsNullOrWhiteSpace(roleName))
                .ToList();

            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToRemove = currentRoles.Except(selectedRoles).ToList();
            var rolesToAdd = selectedRoles.Except(currentRoles).ToList();

            if (rolesToRemove.Count > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    foreach (var error in removeResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                else
                {
                    await LogAuditAsync($"Removed roles from user {user.Email}: {string.Join(", ", rolesToRemove)}");
                }
            }

            if (rolesToAdd.Count > 0)
            {
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    foreach (var error in addResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                else
                {
                    await LogAuditAsync($"Added roles to user {user.Email}: {string.Join(", ", rolesToAdd)}");
                }
            }

            if (!ModelState.IsValid)
            {
                var roles = await _roleManager.Roles
                    .OrderBy(r => r.Name)
                    .ToListAsync();

                model.Roles = roles.Select(role => new UserRoleItemViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name ?? string.Empty,
                    Selected = selectedRoles.Contains(role.Name ?? string.Empty)
                }).ToList();

                model.Email = user.Email ?? user.UserName ?? "User";
                return View(model);
            }

            return RedirectToAction(nameof(Users));
        }

        private static bool IsUserActive(ApplicationUser user)
        {
            if (!user.LockoutEnd.HasValue)
            {
                return true;
            }

            return user.LockoutEnd <= DateTimeOffset.UtcNow;
        }

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var currentUserId = _userManager.GetUserId(User) ?? string.Empty;
            var currentUser = await _userManager.FindByIdAsync(currentUserId);

            if (currentUser?.CompanyID == null)
                return BadRequest("User does not have a company assigned.");

            var company = await _dbContext.Company.FindAsync(currentUser.CompanyID.Value);
            if (company == null)
                return NotFound();

            var model = new CompanySettingsViewModel
            {
                CompanyID = company.CompanyID,
                CompanyName = company.CompanyName
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(CompanySettingsViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var currentUserId = _userManager.GetUserId(User) ?? string.Empty;
            var currentUser = await _userManager.FindByIdAsync(currentUserId);

            if (currentUser?.CompanyID == null)
                return BadRequest("User does not have a company assigned.");

            var company = await _dbContext.Company.FindAsync(currentUser.CompanyID.Value);
            if (company == null)
                return NotFound();

            company.CompanyName = model.CompanyName;
            await _dbContext.SaveChangesAsync();

            await LogAuditAsync($"Updated company name to: {model.CompanyName}");
            await NotifyAdminActionAsync($"Updated company name to: {model.CompanyName}", "Admin", "bi-building-gear");
            TempData["SuccessMessage"] = "Company settings updated successfully.";

            return RedirectToAction(nameof(Settings));
        }

        private async Task LogAuditAsync(string action)
        {
            var currentUserId = _userManager.GetUserId(User) ?? string.Empty;
            var currentUser = await _userManager.FindByIdAsync(currentUserId);

            if (currentUser?.CompanyID == null)
                return;

            var auditLog = new AuditLog
            {
                CompanyID = currentUser.CompanyID.Value,
                UserID = currentUserId,
                Module = "Admin",
                ActionType = "AdminAction",
                Message = action,
                TimeStampUtc = DateTimeOffset.UtcNow
            };

            _dbContext.AuditLog.Add(auditLog);
            await _dbContext.SaveChangesAsync();
        }

        private async Task NotifyAdminActionAsync(string message, string type = "Admin", string icon = "bi-bell")
        {
            var currentUserId = _userManager.GetUserId(User) ?? string.Empty;
            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            if (currentUser?.CompanyID == null) return;

            await _notificationService.NotifyCompanyActionAsync(
                currentUserId, currentUser.CompanyID.Value, message, type, icon);
        }
    }
}
