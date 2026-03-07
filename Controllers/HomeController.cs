using System.Diagnostics;
using IT15_DairyFlow.Data;
using IT15_DairyFlow.Models;
using IT15_DairyFlow.Models.Admin;
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
            var model = new AdminDashboardViewModel();

            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Superadmin"))
            {
                var companies = await _dbContext.Company.Include(c => c.Subscription).ToListAsync();
                model.TotalCompanies = companies.Count;
                model.ActiveSubscriptions = companies.Count(c => c.SubscriptionID != null);
                model.TrialCompanies = companies.Count(c => c.Status == "Trial");
                model.SystemInstances = companies.Count(c => c.Status == "Active");
                model.MonthlyRecurringRevenue = companies
                    .Where(c => c.Subscription != null)
                    .Sum(c => c.Subscription!.Price ?? 0);
            }
            else if (User.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var companyId = currentUser?.CompanyID;

                var now = DateTimeOffset.UtcNow;
                model.TotalUsers = await _userManager.Users.Where(u => u.CompanyID == companyId).CountAsync();
                model.ActiveUsers = await _userManager.Users.Where(u => u.CompanyID == companyId &&
                    (!u.LockoutEnd.HasValue || u.LockoutEnd <= now)).CountAsync();
                model.InactiveUsers = await _userManager.Users.Where(u => u.CompanyID == companyId &&
                    u.LockoutEnd.HasValue && u.LockoutEnd > now).CountAsync();
                model.UsersWithRoles = await _dbContext.UserRoles
                    .Where(ur => _userManager.Users.Where(u => u.CompanyID == companyId).Select(u => u.Id).Contains(ur.UserId))
                    .Select(ur => ur.UserId)
                    .Distinct()
                    .CountAsync();

                var allCompanyUsers = await _userManager.Users.Where(u => u.CompanyID == companyId).ToListAsync();
                var adminCount = 0;
                foreach (var u in allCompanyUsers)
                {
                    if (await _userManager.IsInRoleAsync(u, "Admin")) adminCount++;
                }
                model.AdminUsers = adminCount;
            }

            // ── Build operational dashboard for all authenticated users ──
            if (User.Identity?.IsAuthenticated == true)
            {
                var dash = await BuildUserDashboardAsync();
                ViewBag.UserDashboard = dash;
            }

            return View(model);
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
                .OrderByDescending(a => a.TimeStamp)
                .Take(10)
                .ToListAsync();

            vm.RecentAuditLogs = recentLogs.Select(log =>
            {
                var action = log.Action ?? "";
                var module = DeriveModule(action);
                var status = DeriveStatus(action);

                var elapsed = now - log.TimeStamp;
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
