using System.Diagnostics;
using IT15_DairyFlow.Data;
using IT15_DairyFlow.Models;
using IT15_DairyFlow.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IT15_DairyFlow.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;

        public HomeController(
            ILogger<HomeController> logger,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext)
        {
            _logger = logger;
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index()
        {
            // ── Redirect authenticated users to their role-specific dashboard ──
            if (User.Identity?.IsAuthenticated == true)
            {
                // If user's company has no subscription yet, force plan selection
                if (!User.IsInRole("Superadmin"))
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (currentUser?.CompanyID != null)
                    {
                        var company = await _dbContext.Company
                            .AsNoTracking()
                            .FirstOrDefaultAsync(c => c.CompanyID == currentUser.CompanyID.Value);

                        if (company != null && company.SubscriptionID == null)
                        {
                            return Redirect("/Subscription/Plans");
                        }
                    }
                }

                if (User.IsInRole("Superadmin"))
                    return RedirectToAction("SuperadminDashboard");
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Dashboard", "Admin");
                if (User.IsInRole("ProductManager"))
                    return RedirectToAction("ProductManagerDashboard");
                if (User.IsInRole("QualityChecker"))
                    return RedirectToAction("QualityCheckerDashboard");
                if (User.IsInRole("Finance"))
                    return RedirectToAction("FinanceDashboard");

                // Fallback — authenticated user with no recognized role
                return RedirectToAction("ProductManagerDashboard");
            }

            // Guest landing page
            return View();
        }

        // ───────────────────────────────────────────────────────────
        //  SUPERADMIN DASHBOARD
        // ───────────────────────────────────────────────────────────
        [Authorize(Roles = "Superadmin")]
        public async Task<IActionResult> SuperadminDashboard()
        {
            var model = new AdminDashboardViewModel();
            var companies = await _dbContext.Company.Include(c => c.Subscription).ToListAsync();
            model.TotalCompanies = companies.Count;
            model.ActiveSubscriptions = companies.Count(c => c.SubscriptionID != null);
            model.TrialCompanies = companies.Count(c => c.Status == "Trial");
            model.SystemInstances = companies.Count(c => c.Status == "Active");
            model.MonthlyRecurringRevenue = companies
                .Where(c => c.Subscription != null)
                .Sum(c => c.Subscription!.Price ?? 0);

            return View(model);
        }

        // ───────────────────────────────────────────────────────────
        //  PRODUCT MANAGER DASHBOARD
        // ───────────────────────────────────────────────────────────
        [Authorize(Roles = "Admin,ProductManager")]
        public async Task<IActionResult> ProductManagerDashboard()
        {
            var dash = await BuildUserDashboardAsync();
            return View(dash);
        }

        // ───────────────────────────────────────────────────────────
        //  QUALITY CHECKER DASHBOARD
        // ───────────────────────────────────────────────────────────
        [Authorize(Roles = "Admin,QualityChecker")]
        public async Task<IActionResult> QualityCheckerDashboard()
        {
            var dash = await BuildQualityDashboardAsync();
            return View(dash);
        }

        // ───────────────────────────────────────────────────────────
        //  FINANCE DASHBOARD
        // ───────────────────────────────────────────────────────────
        [Authorize(Roles = "Admin,Finance")]
        public async Task<IActionResult> FinanceDashboard()
        {
            var dash = await BuildFinanceDashboardAsync();
            return View(dash);
        }

        /// <summary>
        /// Populates the operational dashboard (UserDashboardViewModel) with real DB data
        /// scoped to the current user's company.
        /// </summary>
        private async Task<UserDashboardViewModel> BuildUserDashboardAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var companyId = currentUser?.CompanyID;
            var userId = currentUser?.Id ?? string.Empty;
            var vm = new UserDashboardViewModel();

            if (companyId == null) return vm;

            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var sixMonthsAgo = now.AddMonths(-6);

            // ── KPI CARDS ──────────────────────────────────────
            vm.ActiveProducts = await _dbContext.Product
                .CountAsync(p => p.CompanyID == companyId && p.LifecycleStatus == "Active");

            var lastMonthProducts = await _dbContext.Product
                .CountAsync(p => p.CompanyID == companyId && p.LifecycleStatus == "Active");
            // Growth is the ratio over total products
            var totalProducts = await _dbContext.Product.CountAsync(p => p.CompanyID == companyId);
            vm.ActiveProductGrowth = totalProducts > 0
                ? Math.Round((decimal)vm.ActiveProducts / totalProducts * 100, 1)
                : 0;

            vm.OngoingBatches = await _dbContext.ProductionBatch
                .CountAsync(b => b.CompanyID == companyId && b.Status == "In Production");

            vm.BatchesEndingToday = await _dbContext.ProductionBatch
                .CountAsync(b => b.CompanyID == companyId
                    && b.Status == "In Production"
                    && b.EndDate.HasValue
                    && b.EndDate.Value.Date == now.Date);

            vm.TotalInventoryItems = await _dbContext.Inventory
                .CountAsync(i => i.CompanyID == companyId);

            var inventoryItems = await _dbContext.Inventory
                .Where(i => i.CompanyID == companyId)
                .ToListAsync();
            vm.LowStockItems = inventoryItems.Count(i => (i.Quantity ?? 0) < 10);

            vm.StockHealthLabel = vm.LowStockItems == 0 ? "Healthy"
                : vm.LowStockItems <= 3 ? "Fair"
                : "Critical";

            // Production Cost MTD (from expenses this month)
            vm.ProductionCostMTD = await _dbContext.Expense
                .Where(e => e.CompanyID == companyId && e.ExpenseDate >= startOfMonth)
                .SumAsync(e => (decimal?)e.Amount ?? 0);

            var monthlyBudget = await _dbContext.Budget
                .Where(b => b.CompanyID == companyId)
                .SumAsync(b => (decimal?)b.AllocatedAmount ?? 0);
            vm.BudgetVarianceMTD = monthlyBudget > 0
                ? Math.Round(vm.ProductionCostMTD - monthlyBudget, 0)
                : 0;

            // ── PRODUCTION VOLUME CHART (6 months) ─────────────
            var monthlyBatches = await _dbContext.ProductionBatch
                .Where(b => b.CompanyID == companyId && b.StartDate >= sixMonthsAgo)
                .GroupBy(b => new { b.StartDate!.Value.Year, b.StartDate!.Value.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToListAsync();

            // Generate labels for last 6 months
            for (int i = 5; i >= 0; i--)
            {
                var month = now.AddMonths(-i);
                var key = $"{month:yyyy-MM}";
                var shortLabel = month.ToString("MMM");
                vm.ProductionLabels.Add(shortLabel);
                var batch = monthlyBatches.FirstOrDefault(m => m.Year == month.Year && m.Month == month.Month);
                vm.ProductionActual.Add(batch?.Count ?? 0);
            }

            // Target = average of actual batches (simple baseline)
            var avgBatches = vm.ProductionActual.Count > 0
                ? (int)Math.Ceiling(vm.ProductionActual.Where(a => a > 0).DefaultIfEmpty(0).Average())
                : 0;
            vm.ProductionTarget = vm.ProductionActual.Select(_ => avgBatches).ToList();

            // ── QUALITY ASSURANCE ──────────────────────────────
            vm.QualityTotal = await _dbContext.QualityInspection
                .CountAsync(q => q.CompanyID == companyId);
            vm.QualityPassed = await _dbContext.QualityInspection
                .CountAsync(q => q.CompanyID == companyId && q.Result == "pass");
            vm.QualityFailed = await _dbContext.QualityInspection
                .CountAsync(q => q.CompanyID == companyId && q.Result == "fail");
            vm.QualityPending = vm.QualityTotal - vm.QualityPassed - vm.QualityFailed;
            vm.QualityPassRate = vm.QualityTotal > 0
                ? Math.Round((decimal)vm.QualityPassed / vm.QualityTotal * 100, 1)
                : 0;

            // ── INVENTORY ALERTS ───────────────────────────────
            var alerts = new List<InventoryAlertItem>();

            // Expiring inventory (within 7 days)
            var expiringItems = await _dbContext.Inventory
                .Where(i => i.CompanyID == companyId && i.Expiry.HasValue && i.Expiry.Value <= now.AddDays(7) && i.Expiry.Value >= now)
                .Include(i => i.Product)
                .OrderBy(i => i.Expiry)
                .Take(5)
                .ToListAsync();

            foreach (var item in expiringItems)
            {
                var hoursLeft = (item.Expiry!.Value - now).TotalHours;
                alerts.Add(new InventoryAlertItem
                {
                    ProductName = item.Product?.ProductName ?? "Unknown",
                    Detail = $"Qty: {item.Quantity ?? 0}",
                    AlertType = hoursLeft < 24 ? "danger" : "warning",
                    AlertMessage = hoursLeft < 24
                        ? $"Expires in {(int)hoursLeft}h"
                        : $"Expires in {(int)(hoursLeft / 24)}d"
                });
            }

            // Low stock raw materials
            var lowStockMaterials = await _dbContext.RawMaterial
                .Where(r => r.CompanyID == companyId && r.CurrentStock.HasValue && r.MinimumStock.HasValue
                    && r.CurrentStock < r.MinimumStock)
                .Include(r => r.Supplier)
                .OrderBy(r => r.CurrentStock)
                .Take(5)
                .ToListAsync();

            foreach (var mat in lowStockMaterials)
            {
                alerts.Add(new InventoryAlertItem
                {
                    ProductName = mat.MaterialName ?? "Unknown Material",
                    Detail = $"Supplier: {mat.Supplier?.SupplierName ?? "N/A"}",
                    AlertType = "warning",
                    AlertMessage = $"Low Stock ({mat.CurrentStock}{mat.Unit ?? ""})"
                });
            }

            vm.InventoryAlerts = alerts.Take(6).ToList();

            // ── COST BREAKDOWN (all-time expenses) ─────────────
            var allExpenses = await _dbContext.Expense
                .Where(e => e.CompanyID == companyId)
                .ToListAsync();

            vm.TotalExpenses = allExpenses.Sum(e => e.Amount ?? 0);

            // Equipment costs
            vm.CostEquipment = await _dbContext.Equipment
                .Where(e => e.CompanyID == companyId && e.Cost.HasValue)
                .SumAsync(e => (decimal?)e.Cost ?? 0);

            // Raw materials cost (from expenses linked to suppliers) approximate
            vm.CostRawMaterials = allExpenses
                .Where(e => e.SupplierID.HasValue)
                .Sum(e => e.Amount ?? 0);

            vm.CostOther = vm.TotalExpenses - vm.CostRawMaterials;
            vm.CostTotal = vm.TotalExpenses + vm.CostEquipment;

            if (vm.CostTotal > 0)
            {
                vm.CostRawMaterialsPct = Math.Round(vm.CostRawMaterials / vm.CostTotal * 100, 0);
                vm.CostEquipmentPct = Math.Round(vm.CostEquipment / vm.CostTotal * 100, 0);
                vm.CostOtherPct = 100 - vm.CostRawMaterialsPct - vm.CostEquipmentPct;
            }

            // ── REVENUE ────────────────────────────────────────
            vm.TotalRevenue = await _dbContext.BillingInvoice
                .Where(b => b.CompanyID == companyId && b.PaymentStatus == "Paid")
                .SumAsync(b => (decimal?)b.Amount ?? 0);

            // ── RECENT AUDIT TRAIL ─────────────────────────────
            var recentLogs = await _dbContext.AuditLog
                .Where(a => a.CompanyID == companyId)
                .Include(a => a.User)
                .OrderByDescending(a => a.TimeStampUtc)
                .Take(10)
                .ToListAsync();

            vm.RecentAuditLogs = recentLogs.Select(log =>
            {
                var action = string.IsNullOrWhiteSpace(log.Message) ? (log.ActionType ?? "") : log.Message;
                var module = DeriveModule(action);
                var status = DeriveStatus(action);

                var elapsed = now - log.TimeStampUtc.UtcDateTime;
                string timeAgo;
                if (elapsed.TotalMinutes < 1) timeAgo = "Just now";
                else if (elapsed.TotalMinutes < 60) timeAgo = $"{(int)elapsed.TotalMinutes} min ago";
                else if (elapsed.TotalHours < 24) timeAgo = $"{(int)elapsed.TotalHours}h ago";
                else timeAgo = $"{(int)elapsed.TotalDays}d ago";

                return new DashboardAuditItem
                {
                    Activity = action,
                    Staff = log.User?.UserName ?? "Unknown",
                    Module = module,
                    TimeAgo = timeAgo,
                    StatusLabel = status.Label,
                    StatusClass = status.CssClass
                };
            }).ToList();

            return vm;
        }

        // ───────────────────────────────────────────────────────────
        //  BUILD QUALITY CHECKER DASHBOARD DATA
        // ───────────────────────────────────────────────────────────
        private async Task<QualityDashboardViewModel> BuildQualityDashboardAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var companyId = currentUser?.CompanyID;
            var vm = new QualityDashboardViewModel();
            if (companyId == null) return vm;

            var now = DateTime.UtcNow;
            var sixMonthsAgo = now.AddMonths(-6);

            // ── KPI: Inspections ──────────────────────────────
            vm.TotalInspections = await _dbContext.QualityInspection
                .CountAsync(q => q.CompanyID == companyId);
            vm.PassedInspections = await _dbContext.QualityInspection
                .CountAsync(q => q.CompanyID == companyId && q.Result == "pass");
            vm.FailedInspections = await _dbContext.QualityInspection
                .CountAsync(q => q.CompanyID == companyId && q.Result == "fail");
            vm.PendingInspections = vm.TotalInspections - vm.PassedInspections - vm.FailedInspections;
            vm.PassRate = vm.TotalInspections > 0
                ? Math.Round((decimal)vm.PassedInspections / vm.TotalInspections * 100, 1)
                : 0;

            // ── KPI: Non-Conformance Reports ──────────────────
            var ncrs = await _dbContext.NonConformance
                .Where(n => n.CompanyID == companyId)
                .ToListAsync();
            vm.TotalNCRs = ncrs.Count;
            vm.OpenNCRs = ncrs.Count(n => n.Status == "Open" || n.Status == "In Progress");
            vm.ClosedNCRs = ncrs.Count(n => n.Status == "Closed");
            vm.CriticalNCRs = ncrs.Count(n => n.Severity == "Critical" && n.Status != "Closed");

            // ── Inspection Trend (6 months) ───────────────────
            var inspections = await _dbContext.QualityInspection
                .Where(q => q.CompanyID == companyId && q.InspectionDate >= sixMonthsAgo)
                .ToListAsync();

            for (int i = 5; i >= 0; i--)
            {
                var month = now.AddMonths(-i);
                vm.TrendLabels.Add(month.ToString("MMM"));
                vm.TrendPassed.Add(inspections.Count(q => q.InspectionDate.HasValue
                    && q.InspectionDate.Value.Year == month.Year
                    && q.InspectionDate.Value.Month == month.Month
                    && q.Result == "pass"));
                vm.TrendFailed.Add(inspections.Count(q => q.InspectionDate.HasValue
                    && q.InspectionDate.Value.Year == month.Year
                    && q.InspectionDate.Value.Month == month.Month
                    && q.Result == "fail"));
            }

            // ── Finished Goods Inventory ──────────────────────
            vm.FinishedGoodsCount = await _dbContext.Inventory
                .CountAsync(i => i.CompanyID == companyId);
            var expiringItems = await _dbContext.Inventory
                .Where(i => i.CompanyID == companyId && i.Expiry.HasValue && i.Expiry.Value <= now.AddDays(7) && i.Expiry.Value >= now)
                .Include(i => i.Product)
                .OrderBy(i => i.Expiry)
                .Take(5)
                .ToListAsync();

            vm.ExpiringItems = expiringItems.Count;
            vm.ExpiryAlerts = expiringItems.Select(item =>
            {
                var hoursLeft = (item.Expiry!.Value - now).TotalHours;
                return new InventoryAlertItem
                {
                    ProductName = item.Product?.ProductName ?? "Unknown",
                    Detail = $"Qty: {item.Quantity ?? 0}",
                    AlertType = hoursLeft < 24 ? "danger" : "warning",
                    AlertMessage = hoursLeft < 24
                        ? $"Expires in {(int)hoursLeft}h"
                        : $"Expires in {(int)(hoursLeft / 24)}d"
                };
            }).ToList();

            // ── Recent QM Audit Trail ─────────────────────────
            var recentLogs = await _dbContext.AuditLog
                .Where(a => a.CompanyID == companyId)
                .Include(a => a.User)
                .OrderByDescending(a => a.TimeStampUtc)
                .Take(10)
                .ToListAsync();

            vm.RecentAuditLogs = recentLogs
                .Where(log =>
                {
                    var lower = (string.IsNullOrWhiteSpace(log.Message) ? (log.ActionType ?? "") : log.Message).ToLower();
                    return lower.Contains("quality") || lower.Contains("inspection")
                        || lower.Contains("ncr") || lower.Contains("non-conformance")
                        || lower.Contains("hold") || lower.Contains("release")
                        || lower.Contains("inventory");
                })
                .Select(log => BuildAuditItem(log, now))
                .Take(8)
                .ToList();

            return vm;
        }

        // ───────────────────────────────────────────────────────────
        //  BUILD FINANCE DASHBOARD DATA
        // ───────────────────────────────────────────────────────────
        private async Task<FinanceDashboardViewModel> BuildFinanceDashboardAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var companyId = currentUser?.CompanyID;
            var vm = new FinanceDashboardViewModel();
            if (companyId == null) return vm;

            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var sixMonthsAgo = now.AddMonths(-6);

            // ── KPI: Revenue & Expenses ───────────────────────
            vm.TotalRevenue = await _dbContext.BillingInvoice
                .Where(b => b.CompanyID == companyId && b.PaymentStatus == "Paid")
                .SumAsync(b => (decimal?)b.Amount ?? 0);

            var allExpenses = await _dbContext.Expense
                .Where(e => e.CompanyID == companyId)
                .ToListAsync();
            vm.TotalExpenses = allExpenses.Sum(e => e.Amount ?? 0);

            // ── Budget ────────────────────────────────────────
            vm.TotalBudget = await _dbContext.Budget
                .Where(b => b.CompanyID == companyId)
                .SumAsync(b => (decimal?)b.AllocatedAmount ?? 0);
            vm.ExpensesMTD = await _dbContext.Expense
                .Where(e => e.CompanyID == companyId && e.ExpenseDate >= startOfMonth)
                .SumAsync(e => (decimal?)e.Amount ?? 0);
            vm.BudgetUtilization = vm.TotalBudget > 0
                ? Math.Round(vm.ExpensesMTD / vm.TotalBudget * 100, 1)
                : 0;

            // ── Invoices ──────────────────────────────────────
            var invoices = await _dbContext.BillingInvoice
                .Where(b => b.CompanyID == companyId)
                .ToListAsync();
            vm.TotalInvoices = invoices.Count;
            vm.PaidInvoices = invoices.Count(i => i.PaymentStatus == "Paid");
            vm.PendingInvoices = invoices.Count(i => i.PaymentStatus == "Pending");
            vm.PendingAmount = invoices
                .Where(i => i.PaymentStatus == "Pending")
                .Sum(i => i.Amount ?? 0);

            // ── Trend (6 months) ──────────────────────────────
            var expenses = await _dbContext.Expense
                .Where(e => e.CompanyID == companyId && e.ExpenseDate >= sixMonthsAgo)
                .ToListAsync();
            var revenues = await _dbContext.BillingInvoice
                .Where(b => b.CompanyID == companyId && b.PaymentStatus == "Paid" && b.InvoiceDate >= sixMonthsAgo)
                .ToListAsync();

            for (int i = 5; i >= 0; i--)
            {
                var month = now.AddMonths(-i);
                vm.TrendLabels.Add(month.ToString("MMM"));
                vm.TrendRevenue.Add(revenues
                    .Where(r => r.InvoiceDate.HasValue && r.InvoiceDate.Value.Year == month.Year && r.InvoiceDate.Value.Month == month.Month)
                    .Sum(r => r.Amount ?? 0));
                vm.TrendExpenses.Add(expenses
                    .Where(e => e.ExpenseDate.HasValue && e.ExpenseDate.Value.Year == month.Year && e.ExpenseDate.Value.Month == month.Month)
                    .Sum(e => e.Amount ?? 0));
            }

            // ── Expense Breakdown ─────────────────────────────
            vm.SupplierExpenses = allExpenses.Where(e => e.SupplierID.HasValue).Sum(e => e.Amount ?? 0);
            vm.EquipmentCosts = await _dbContext.Equipment
                .Where(e => e.CompanyID == companyId && e.Cost.HasValue)
                .SumAsync(e => (decimal?)e.Cost ?? 0);
            vm.OtherExpenses = vm.TotalExpenses - vm.SupplierExpenses;

            // ── Recent Finance Audit Trail ─────────────────────
            var recentLogs = await _dbContext.AuditLog
                .Where(a => a.CompanyID == companyId)
                .Include(a => a.User)
                .OrderByDescending(a => a.TimeStampUtc)
                .Take(20)
                .ToListAsync();

            vm.RecentAuditLogs = recentLogs
                .Where(log =>
                {
                    var lower = (string.IsNullOrWhiteSpace(log.Message) ? (log.ActionType ?? "") : log.Message).ToLower();
                    return lower.Contains("expense") || lower.Contains("budget")
                        || lower.Contains("invoice") || lower.Contains("finance")
                        || lower.Contains("payment");
                })
                .Select(log => BuildAuditItem(log, now))
                .Take(8)
                .ToList();

            return vm;
        }

        // ── Shared audit item builder ─────────────────────────────
        private DashboardAuditItem BuildAuditItem(AuditLog log, DateTime now)
        {
            var action = string.IsNullOrWhiteSpace(log.Message) ? (log.ActionType ?? "") : log.Message;
            var module = DeriveModule(action);
            var status = DeriveStatus(action);
            var elapsed = now - log.TimeStampUtc.UtcDateTime;
            string timeAgo;
            if (elapsed.TotalMinutes < 1) timeAgo = "Just now";
            else if (elapsed.TotalMinutes < 60) timeAgo = $"{(int)elapsed.TotalMinutes} min ago";
            else if (elapsed.TotalHours < 24) timeAgo = $"{(int)elapsed.TotalHours}h ago";
            else timeAgo = $"{(int)elapsed.TotalDays}d ago";

            return new DashboardAuditItem
            {
                Activity = action,
                Staff = log.User?.UserName ?? "Unknown",
                Module = module,
                TimeAgo = timeAgo,
                StatusLabel = status.Label,
                StatusClass = status.CssClass
            };
        }

        private static string DeriveModule(string action)
        {
            var lower = action.ToLower();
            if (lower.Contains("batch") || lower.Contains("production")) return "Production";
            if (lower.Contains("quality") || lower.Contains("inspection")) return "Quality";
            if (lower.Contains("product") || lower.Contains("formulation")) return "PLM";
            if (lower.Contains("inventory") || lower.Contains("raw material") || lower.Contains("stock")) return "Inventory";
            if (lower.Contains("equipment")) return "Equipment";
            if (lower.Contains("expense") || lower.Contains("budget") || lower.Contains("invoice") || lower.Contains("finance")) return "Finance";
            if (lower.Contains("user") || lower.Contains("role")) return "Admin";
            if (lower.Contains("supplier")) return "Suppliers";
            return "System";
        }

        private static (string Label, string CssClass) DeriveStatus(string action)
        {
            var lower = action.ToLower();
            if (lower.Contains("created") || lower.Contains("added") || lower.Contains("started"))
                return ("Active", "bg-soft-primary text-primary");
            if (lower.Contains("completed") || lower.Contains("passed") || lower.Contains("approved") || lower.Contains("released"))
                return ("Passed", "bg-soft-success text-success");
            if (lower.Contains("failed") || lower.Contains("rejected") || lower.Contains("deleted"))
                return ("Failed", "bg-soft-danger text-danger");
            if (lower.Contains("updated") || lower.Contains("toggled") || lower.Contains("edited"))
                return ("Updated", "bg-soft-info text-info");
            return ("Logged", "bg-soft-primary text-primary");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [Authorize]
        public IActionResult HelpSupport()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
