using IT15_DairyFlow.Models.Admin;
using IT15_DairyFlow.Models;
using IT15_DairyFlow.Data;
using IT15_DairyFlow.Services;
using IT15_DairyFlow.Services.Security;
using IT15_DairyFlow.Models.SuperAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IT15_DairyFlow.Security.Crypto;

namespace IT15_DairyFlow.Controllers
{
    [Authorize(Roles = "Superadmin")]
    public class SuperAdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly NotificationService _notificationService;
        private readonly ICryptoService _crypto;
        private readonly IEncryptionBackfillService _encryptionBackfill;

        public SuperAdminController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            NotificationService notificationService,
            ICryptoService crypto,
            IEncryptionBackfillService encryptionBackfill)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _notificationService = notificationService;
            _crypto = crypto;
            _encryptionBackfill = encryptionBackfill;
        }

        // ─── APP-LAYER ENCRYPTION BACKFILL (BUSINESS TABLES) ─────
        [HttpGet]
        public IActionResult BackfillEncryptedBusinessData()
        {
            return View(new BusinessDataEncryptionBackfillViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BackfillEncryptedBusinessData(BusinessDataEncryptionBackfillViewModel input)
        {
            if (input.DryRun)
            {
                input.Message = input.CompanyID.HasValue
                    ? $"Dry run: would backfill encrypted columns for CompanyID={input.CompanyID.Value}."
                    : "Dry run: would backfill encrypted columns for ALL companies.";
                return View(input);
            }

            var result = await _encryptionBackfill.BackfillAsync(input.CompanyID);

            input.Result = result;
            input.Message = $"Backfill completed. Products={result.ProductsUpdated}, Batches={result.ProductionBatchesUpdated}, QI={result.QualityInspectionsUpdated}, NCR={result.NonConformancesUpdated}, Equipment={result.EquipmentsUpdated}, Sales={result.SalesUpdated}, Txns={result.SaleTransactionsUpdated}.";

            await LogAuditAsync(
                $"Business-data encryption backfill ran. CompanyID={(input.CompanyID.HasValue ? input.CompanyID.Value.ToString() : "ALL")}. " +
                input.Message);

            await _notificationService.NotifySuperAdminsAsync(
                "Business data encryption backfill completed.",
                "System",
                "bi-shield-lock",
                _userManager.GetUserId(User));

            return View(input);
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
            var emailLookup = _crypto.ComputeLookupHash(addModel.AdminEmail);
            var existingUser = await _userManager.Users.FirstOrDefaultAsync(u => u.EmailLookupHash == emailLookup);
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
                EmailConfirmed = true,
                CompanyID = company.CompanyID
            };

            UserEmailProtector.ProtectEmail(user, _crypto, addModel.AdminEmail);

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

        // ─── LOGS (SEPARATED) ─────────────────────────────────────

        /// <summary>
        /// Platform operational/technical logs (health + performance). Not user audit logs.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SystemLogs(
            DateTime? startDateUtc = null,
            DateTime? endDateUtc = null,
            string? level = null,
            string? component = null,
            string? eventName = null,
            int? companyId = null,
            string? correlationId = null)
        {
            var query = _dbContext.SystemLogs.AsQueryable();

            if (startDateUtc.HasValue)
                query = query.Where(s => s.TimeStampUtc >= new DateTimeOffset(startDateUtc.Value, TimeSpan.Zero));
            if (endDateUtc.HasValue)
                query = query.Where(s => s.TimeStampUtc <= new DateTimeOffset(endDateUtc.Value.AddDays(1), TimeSpan.Zero));
            if (!string.IsNullOrWhiteSpace(level))
                query = query.Where(s => s.Level == level);
            if (!string.IsNullOrWhiteSpace(component))
                query = query.Where(s => s.Component.Contains(component));
            if (!string.IsNullOrWhiteSpace(eventName))
                query = query.Where(s => s.EventName.Contains(eventName));
            if (companyId.HasValue)
                query = query.Where(s => s.CompanyId == companyId);
            if (!string.IsNullOrWhiteSpace(correlationId))
                query = query.Where(s => s.CorrelationId == correlationId);

            // Keep the page reasonably small; can be extended to pagination later.
            var logs = await query
                .OrderByDescending(s => s.TimeStampUtc)
                .Take(1000)
                .ToListAsync();

            var vm = new SystemLogFilterViewModel
            {
                StartDateUtc = startDateUtc,
                EndDateUtc = endDateUtc,
                Level = level,
                Component = component,
                EventName = eventName,
                CompanyId = companyId,
                CorrelationId = correlationId,
                Logs = logs.Select(s => new SystemLogListItemViewModel
                {
                    SystemLogId = s.SystemLogId,
                    TimeStampUtc = s.TimeStampUtc,
                    Level = s.Level,
                    Component = s.Component,
                    EventName = s.EventName,
                    Message = s.Message,
                    CompanyId = s.CompanyId,
                    UserId = s.UserId,
                    DurationMs = s.DurationMs,
                    CorrelationId = s.CorrelationId
                }).ToList(),
                TotalRecords = logs.Count
            };

            return View(vm);
        }

        /// <summary>
        /// User/business action audit logs across all companies (transparency).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AuditLogs(
            DateTime? startDateUtc = null,
            DateTime? endDateUtc = null,
            int? companyId = null,
            string? userFilter = null,
            string? actionFilter = null,
            string? moduleFilter = null)
        {
            var query = _dbContext.AuditLog
                .Include(a => a.User)
                .AsQueryable();

            if (startDateUtc.HasValue)
                query = query.Where(a => a.TimeStampUtc >= new DateTimeOffset(startDateUtc.Value, TimeSpan.Zero));
            if (endDateUtc.HasValue)
                query = query.Where(a => a.TimeStampUtc <= new DateTimeOffset(endDateUtc.Value.AddDays(1), TimeSpan.Zero));
            if (companyId.HasValue)
                query = query.Where(a => a.CompanyID == companyId);
            if (!string.IsNullOrWhiteSpace(userFilter))
                query = query.Where(a => a.UserID == userFilter || (a.User != null && (a.User.UserName!.Contains(userFilter) || a.User.Email!.Contains(userFilter))));
            if (!string.IsNullOrWhiteSpace(actionFilter))
                query = query.Where(a => (a.Message ?? a.ActionType).Contains(actionFilter));
            if (!string.IsNullOrWhiteSpace(moduleFilter))
                query = query.Where(a => a.Module.Contains(moduleFilter));

            var logs = await query
                .OrderByDescending(a => a.TimeStampUtc)
                .Take(2000)
                .ToListAsync();

            var vm = new IT15_DairyFlow.Models.Admin.AuditLogFilterViewModel
            {
                StartDate = startDateUtc,
                EndDate = endDateUtc,
                ActionFilter = actionFilter,
                UserFilter = userFilter,
                AuditLogs = logs.Select(log => new IT15_DairyFlow.Models.Admin.AuditLogViewModel
                {
                    AuditLogID = log.AuditLogID,
                    UserName = log.User?.UserName ?? log.User?.Email ?? log.UserID,
                    Action = string.IsNullOrWhiteSpace(log.Message) ? (log.ActionType ?? "Unknown") : log.Message,
                    TimeStamp = log.TimeStampUtc.UtcDateTime
                }).ToList(),
                TotalRecords = logs.Count
            };

            ViewBag.CompanyId = companyId;
            ViewBag.ModuleFilter = moduleFilter;

            return View(vm);
        }

        // ─── EMAIL BACKFILL (SECURITY UPGRADE) ─────────────────────
        [HttpGet]
        public async Task<IActionResult> BackfillProtectedEmails()
        {
            var total = await _userManager.Users.CountAsync();
            var needing = await _userManager.Users.CountAsync(u => u.EmailLookupHash == null);

            var vm = new EmailBackfillViewModel
            {
                TotalUsers = total,
                UsersNeedingBackfill = needing,
                Updated = 0,
                DryRun = true,
                BatchSize = 200
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BackfillProtectedEmails(EmailBackfillViewModel input)
        {
            var batchSize = input.BatchSize <= 0 ? 200 : Math.Min(input.BatchSize, 2000);
            var dryRun = input.DryRun;

            var query = _userManager.Users
                .Where(u => u.EmailLookupHash == null)
                .OrderBy(u => u.Id)
                .Take(batchSize);

            var users = await query.ToListAsync();
            var updated = 0;

            foreach (var u in users)
            {
                if (string.IsNullOrWhiteSpace(u.Email))
                {
                    continue;
                }

                // Fill protected columns
                u.EmailLookupHash = _crypto.ComputeLookupHash(u.Email);
                u.EmailEncrypted = _crypto.EncryptToBase64(u.Email);

                if (!dryRun)
                {
                    // Use UserManager to ensure Identity normalization behaviors
                    var result = await _userManager.UpdateAsync(u);
                    if (result.Succeeded)
                    {
                        updated++;
                    }
                }
                else
                {
                    updated++;
                }
            }

            var total = await _userManager.Users.CountAsync();
            var needing = await _userManager.Users.CountAsync(u => u.EmailLookupHash == null);

            if (!dryRun)
            {
                await LogAuditAsync($"Backfilled protected emails for {updated} user(s). BatchSize={batchSize}.");
                await _notificationService.NotifySuperAdminsAsync(
                    $"Protected email backfill completed: {updated} user(s) updated.",
                    "System",
                    "bi-shield-lock",
                    _userManager.GetUserId(User));
            }

            var vm = new EmailBackfillViewModel
            {
                TotalUsers = total,
                UsersNeedingBackfill = needing,
                Updated = updated,
                BatchSize = batchSize,
                DryRun = dryRun,
                Message = dryRun
                    ? $"Dry run: would update {updated} user(s). Unprotected remaining: {needing}."
                    : $"Updated {updated} user(s). Unprotected remaining: {needing}."
            };

            return View(vm);
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
                Module = "Admin",
                ActionType = "SuperAdminAction",
                Message = action,
                TimeStampUtc = DateTimeOffset.UtcNow
            });
            await _dbContext.SaveChangesAsync();
        }
    }
}
