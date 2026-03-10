using IT15_DairyFlow.Data;
using IT15_DairyFlow.Hubs;
using IT15_DairyFlow.Models;
using IT15_DairyFlow.Models.Finance;
using IT15_DairyFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace IT15_DairyFlow.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class FinanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<DairyFlowHub> _hub;
        private readonly NotificationService _notificationService;

        public FinanceController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IHubContext<DairyFlowHub> hub, NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _hub = hub;
            _notificationService = notificationService;
        }

        private async Task<int> GetCompanyIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.CompanyID ?? 1;
        }

        // GET: Finance/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var companyId = await GetCompanyIdAsync();
            var currentPeriod = DateTime.UtcNow.ToString("yyyy-MM");
            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            // Get expenses
            var expenses = await _context.Expense
                .Include(e => e.Supplier)
                .Include(e => e.User)
                .Where(e => e.CompanyID == companyId)
                .OrderByDescending(e => e.ExpenseDate)
                .Take(10)
                .Select(e => new ExpenseListViewModel
                {
                    ExpenseID = e.ExpenseID,
                    Amount = e.Amount ?? 0,
                    ExpenseDate = e.ExpenseDate,
                    SupplierName = e.Supplier != null ? (e.Supplier.SupplierName ?? "N/A") : "N/A",
                    Category = e.Category ?? "Other",
                    UserName = e.User.UserName ?? e.User.Email ?? "Unknown",
                    IsEmergency = e.IsEmergency,
                    EmergencyReason = e.EmergencyReason
                })
                .ToListAsync();

            // Get all budgets
            var budgets = await _context.Budget
                .Where(b => b.CompanyID == companyId)
                .OrderByDescending(b => b.Period).ThenBy(b => b.Category)
                .Select(b => new BudgetListViewModel
                {
                    BudgetID = b.BudgetID,
                    Period = b.Period ?? "N/A",
                    Category = b.Category ?? "Other",
                    AllocatedAmount = b.AllocatedAmount ?? 0
                })
                .ToListAsync();

            // Calculate spent per budget (by matching period + category)
            foreach (var budget in budgets)
            {
                // Parse budget period to get month range
                if (DateTime.TryParse(budget.Period + "-01", out var budgetMonth))
                {
                    var bStart = new DateTime(budgetMonth.Year, budgetMonth.Month, 1);
                    var bEnd = bStart.AddMonths(1);

                    budget.SpentAmount = await _context.Expense
                        .Where(e => e.CompanyID == companyId
                            && e.Category == budget.Category
                            && e.ExpenseDate.HasValue
                            && e.ExpenseDate.Value >= bStart
                            && e.ExpenseDate.Value < bEnd)
                        .SumAsync(e => (decimal?)e.Amount ?? 0);

                    budget.EmergencyCount = await _context.Expense
                        .CountAsync(e => e.CompanyID == companyId
                            && e.Category == budget.Category
                            && e.IsEmergency
                            && e.ExpenseDate.HasValue
                            && e.ExpenseDate.Value >= bStart
                            && e.ExpenseDate.Value < bEnd);
                }

                budget.RemainingAmount = budget.AllocatedAmount - budget.SpentAmount;
                budget.IsOverBudget = budget.SpentAmount > budget.AllocatedAmount;
                budget.UtilizationPercentage = budget.AllocatedAmount > 0
                    ? Math.Min(100, (budget.SpentAmount / budget.AllocatedAmount) * 100)
                    : 0;
            }

            // Current month budget status
            var currentMonthBudgets = await _context.Budget
                .Where(b => b.CompanyID == companyId && b.Period == currentPeriod)
                .ToListAsync();

            var monthlyBudgetStatuses = new List<MonthlyBudgetStatusViewModel>();
            foreach (var b in currentMonthBudgets)
            {
                var spent = await _context.Expense
                    .Where(e => e.CompanyID == companyId
                        && e.Category == b.Category
                        && e.ExpenseDate.HasValue
                        && e.ExpenseDate.Value >= monthStart
                        && e.ExpenseDate.Value < monthEnd)
                    .SumAsync(e => (decimal?)e.Amount ?? 0);

                var emergencyCount = await _context.Expense
                    .CountAsync(e => e.CompanyID == companyId
                        && e.Category == b.Category
                        && e.IsEmergency
                        && e.ExpenseDate.HasValue
                        && e.ExpenseDate.Value >= monthStart
                        && e.ExpenseDate.Value < monthEnd);

                var emergencyTotal = await _context.Expense
                    .Where(e => e.CompanyID == companyId
                        && e.Category == b.Category
                        && e.IsEmergency
                        && e.ExpenseDate.HasValue
                        && e.ExpenseDate.Value >= monthStart
                        && e.ExpenseDate.Value < monthEnd)
                    .SumAsync(e => (decimal?)e.Amount ?? 0);

                monthlyBudgetStatuses.Add(new MonthlyBudgetStatusViewModel
                {
                    BudgetID = b.BudgetID,
                    Category = b.Category ?? "Other",
                    Period = currentPeriod,
                    AllocatedAmount = b.AllocatedAmount ?? 0,
                    SpentAmount = spent,
                    RemainingAmount = (b.AllocatedAmount ?? 0) - spent,
                    UtilizationPercentage = (b.AllocatedAmount ?? 0) > 0
                        ? Math.Min(100, (spent / (b.AllocatedAmount ?? 1)) * 100) : 0,
                    IsOverBudget = spent > (b.AllocatedAmount ?? 0),
                    EmergencyCount = emergencyCount,
                    EmergencyTotal = emergencyTotal
                });
            }

            // Calculate total expenses
            var totalExpenses = await _context.Expense
                .Where(e => e.CompanyID == companyId)
                .SumAsync(e => (decimal?)e.Amount ?? 0);

            // Get billing invoices
            var invoices = await _context.BillingInvoice
                .Where(b => b.CompanyID == companyId)
                .OrderByDescending(b => b.InvoiceDate)
                .Take(10)
                .Select(b => new BillingInvoiceListViewModel
                {
                    BillingInvoiceID = b.BillingInvoiceID,
                    Amount = b.Amount ?? 0,
                    InvoiceDate = b.InvoiceDate,
                    DueDate = b.DueDate,
                    PaymentStatus = b.PaymentStatus ?? "Pending"
                })
                .ToListAsync();

            // Get production costs
            var productionCosts = await _context.ProductionCost
                .Include(pc => pc.ProductionBatch)
                .Where(pc => pc.CompanyID == companyId)
                .OrderByDescending(pc => pc.ProductionCostID)
                .Take(10)
                .Select(pc => new ProductionCostViewModel
                {
                    ProductionCostID = pc.ProductionCostID,
                    ProductionBatchID = pc.ProductionBatchID,
                    BatchCode = pc.ProductionBatch != null ? (pc.ProductionBatch.BatchCode ?? "N/A") : "N/A",
                    TotalCost = pc.TotalCost ?? 0,
                    StartDate = pc.ProductionBatch != null ? pc.ProductionBatch.StartDate : null,
                    EndDate = pc.ProductionBatch != null ? pc.ProductionBatch.EndDate : null
                })
                .ToListAsync();

            // Get suppliers
            var suppliers = await _context.Supplier
                .Where(s => s.CompanyID == companyId)
                .OrderBy(s => s.SupplierName)
                .Select(s => new SupplierLookupViewModel
                {
                    Id = s.SupplierID,
                    Name = s.SupplierName ?? "Unknown"
                })
                .ToListAsync();

            // Calculate summary
            var totalRevenue = await _context.BillingInvoice
                .Where(b => b.CompanyID == companyId && b.PaymentStatus == "Paid")
                .SumAsync(b => (decimal?)b.Amount ?? 0);

            // Include sales revenue
            var salesRevenue = await _context.Sale
                .Where(s => s.CompanyID == companyId && s.PaymentStatus == "Paid")
                .SumAsync(s => (decimal?)s.TotalAmount ?? 0);
            totalRevenue += salesRevenue;

            var totalBudget = await _context.Budget
                .Where(b => b.CompanyID == companyId)
                .SumAsync(b => (decimal?)b.AllocatedAmount ?? 0);

            var totalProductionCosts = await _context.ProductionCost
                .Where(pc => pc.CompanyID == companyId)
                .SumAsync(pc => (decimal?)pc.TotalCost ?? 0);

            var pendingInvoices = await _context.BillingInvoice
                .Where(b => b.CompanyID == companyId && b.PaymentStatus == "Pending")
                .SumAsync(b => (decimal?)b.Amount ?? 0);

            var summary = new FinancialSummaryViewModel
            {
                TotalRevenue = totalRevenue,
                TotalExpenses = totalExpenses,
                NetIncome = totalRevenue - totalExpenses,
                TotalBudget = totalBudget,
                BudgetUtilization = totalBudget > 0 ? Math.Min(100, (totalExpenses / totalBudget) * 100) : 0,
                ProductionCosts = totalProductionCosts,
                PendingInvoices = pendingInvoices,
                ExpenseCount = await _context.Expense.CountAsync(e => e.CompanyID == companyId),
                InvoiceCount = await _context.BillingInvoice.CountAsync(b => b.CompanyID == companyId)
            };

            var viewModel = new FinanceDashboardViewModel
            {
                Summary = summary,
                RecentExpenses = expenses,
                Budgets = budgets,
                RecentInvoices = invoices,
                ProductionCosts = productionCosts,
                Suppliers = suppliers,
                MonthlyBudgets = monthlyBudgetStatuses,
                CurrentPeriod = currentPeriod
            };

            return View(viewModel);
        }

        // POST: Finance/CreateExpense
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var companyId = await GetCompanyIdAsync();
            var expenseDate = model.ExpenseDate ?? DateTime.UtcNow;
            var period = expenseDate.ToString("yyyy-MM");

            // Budget enforcement: check if category has a monthly budget limit
            var budget = await _context.Budget
                .FirstOrDefaultAsync(b => b.CompanyID == companyId && b.Period == period && b.Category == model.Category);

            bool isEmergency = false;
            if (budget != null)
            {
                var monthStart = new DateTime(expenseDate.Year, expenseDate.Month, 1);
                var monthEnd = monthStart.AddMonths(1);

                var spentThisMonth = await _context.Expense
                    .Where(e => e.CompanyID == companyId
                        && e.Category == model.Category
                        && e.ExpenseDate.HasValue
                        && e.ExpenseDate.Value >= monthStart
                        && e.ExpenseDate.Value < monthEnd)
                    .SumAsync(e => (decimal?)e.Amount ?? 0);

                var remaining = (budget.AllocatedAmount ?? 0) - spentThisMonth;

                if (model.Amount > remaining)
                {
                    // Over budget — require emergency flag
                    if (!model.IsEmergency)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            overBudget = true,
                            message = $"This expense exceeds the {model.Category} budget for {period}. " +
                                      $"Budget: ₱{budget.AllocatedAmount:N2} | Spent: ₱{spentThisMonth:N2} | Remaining: ₱{remaining:N2}. " +
                                      $"Mark as emergency to proceed.",
                            allocated = budget.AllocatedAmount ?? 0,
                            spent = spentThisMonth,
                            remainingAmount = remaining
                        });
                    }
                    isEmergency = true;
                }
            }

            var expense = new Expense
            {
                CompanyID = companyId,
                UserID = user.Id,
                Amount = model.Amount,
                ExpenseDate = expenseDate,
                SupplierID = model.SupplierID,
                Category = model.Category,
                IsEmergency = isEmergency,
                EmergencyReason = isEmergency ? model.EmergencyReason : null
            };

            _context.Expense.Add(expense);
            await _context.SaveChangesAsync();

            // SignalR: notify expense created
            var label = isEmergency ? $"EMERGENCY {model.Category}" : model.Category;
            await _hub.NotifyExpenseCreated(companyId, label, model.Amount, user.UserName ?? "");
            await _hub.NotifyDashboardRefresh(companyId, "Finance");
            var msg = isEmergency
                ? $"⚠️ Emergency expense recorded ({model.Category}): ₱{model.Amount:N2} — {model.EmergencyReason}"
                : $"New expense recorded ({model.Category}): ₱{model.Amount:N2}";
            await _notificationService.NotifyCompanyActionAsync(
                user.Id, companyId, msg, "Finance", isEmergency ? "bi-exclamation-triangle" : "bi-cash-stack");

            return Ok(new
            {
                success = true,
                message = isEmergency
                    ? "Emergency expense recorded (over budget)."
                    : "Expense recorded successfully.",
                isEmergency
            });
        }

        // POST: Finance/UpdateExpense
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateExpense([FromBody] UpdateExpenseViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var companyId = await GetCompanyIdAsync();

            var expense = await _context.Expense
                .FirstOrDefaultAsync(e => e.ExpenseID == model.ExpenseID && e.CompanyID == companyId);

            if (expense == null)
            {
                return NotFound();
            }

            expense.Amount = model.Amount;
            expense.ExpenseDate = model.ExpenseDate;
            expense.SupplierID = model.SupplierID;
            expense.Category = model.Category;

            _context.Expense.Update(expense);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Expense updated successfully." });
        }

        // POST: Finance/DeleteExpense/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            var companyId = await GetCompanyIdAsync();

            var expense = await _context.Expense
                .FirstOrDefaultAsync(e => e.ExpenseID == id && e.CompanyID == companyId);

            if (expense == null)
            {
                return NotFound();
            }

            _context.Expense.Remove(expense);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Expense deleted successfully." });
        }

        // GET: Finance/GetExpenseDetail/{id}
        [HttpGet]
        public async Task<IActionResult> GetExpenseDetail(int id)
        {
            var companyId = await GetCompanyIdAsync();

            var expense = await _context.Expense
                .Include(e => e.Supplier)
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.ExpenseID == id && e.CompanyID == companyId);

            if (expense == null)
            {
                return NotFound();
            }

            var viewModel = new ExpenseDetailViewModel
            {
                ExpenseID = expense.ExpenseID,
                Amount = expense.Amount ?? 0,
                ExpenseDate = expense.ExpenseDate,
                SupplierID = expense.SupplierID,
                SupplierName = expense.Supplier?.SupplierName ?? "N/A",
                UserName = expense.User?.UserName ?? expense.User?.Email ?? "Unknown",
                UserID = expense.UserID,
                Category = expense.Category ?? "Other",
                IsEmergency = expense.IsEmergency,
                EmergencyReason = expense.EmergencyReason
            };

            return Json(viewModel);
        }

        // POST: Finance/CreateBudget
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBudget([FromBody] CreateBudgetViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var companyId = await GetCompanyIdAsync();

            // Check if budget for the period + category already exists
            var existingBudget = await _context.Budget
                .FirstOrDefaultAsync(b => b.CompanyID == companyId && b.Period == model.Period && b.Category == model.Category);

            if (existingBudget != null)
            {
                return BadRequest(new { success = false, message = $"A budget for {model.Category} in {model.Period} already exists." });
            }

            var budget = new Budget
            {
                CompanyID = companyId,
                Period = model.Period,
                Category = model.Category,
                AllocatedAmount = model.AllocatedAmount
            };

            _context.Budget.Add(budget);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = $"Budget limit set: ₱{model.AllocatedAmount:N2} for {model.Category} in {model.Period}." });
        }

        // POST: Finance/UpdateBudget
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBudget([FromBody] UpdateBudgetViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var companyId = await GetCompanyIdAsync();

            var budget = await _context.Budget
                .FirstOrDefaultAsync(b => b.BudgetID == model.BudgetID && b.CompanyID == companyId);

            if (budget == null)
            {
                return NotFound();
            }

            budget.Period = model.Period;
            budget.Category = model.Category;
            budget.AllocatedAmount = model.AllocatedAmount;

            _context.Budget.Update(budget);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Budget updated successfully." });
        }

        // POST: Finance/DeleteBudget/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBudget(int id)
        {
            var companyId = await GetCompanyIdAsync();

            var budget = await _context.Budget
                .FirstOrDefaultAsync(b => b.BudgetID == id && b.CompanyID == companyId);

            if (budget == null)
            {
                return NotFound();
            }

            _context.Budget.Remove(budget);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Budget deleted successfully." });
        }

        // POST: Finance/CreateInvoice
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInvoice([FromBody] CreateBillingInvoiceViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var companyId = await GetCompanyIdAsync();

            var invoice = new BillingInvoice
            {
                CompanyID = companyId,
                Amount = model.Amount,
                InvoiceDate = model.InvoiceDate ?? DateTime.UtcNow,
                DueDate = model.DueDate,
                PaymentStatus = model.PaymentStatus ?? "Pending"
            };

            _context.BillingInvoice.Add(invoice);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Invoice created successfully." });
        }

        // POST: Finance/UpdateInvoice
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInvoice([FromBody] UpdateBillingInvoiceViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var companyId = await GetCompanyIdAsync();

            var invoice = await _context.BillingInvoice
                .FirstOrDefaultAsync(i => i.BillingInvoiceID == model.BillingInvoiceID && i.CompanyID == companyId);

            if (invoice == null)
            {
                return NotFound();
            }

            invoice.Amount = model.Amount;
            invoice.DueDate = model.DueDate;
            invoice.PaymentStatus = model.PaymentStatus;

            _context.BillingInvoice.Update(invoice);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Invoice updated successfully." });
        }

        // POST: Finance/MarkInvoicePaid/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkInvoicePaid(int id)
        {
            var companyId = await GetCompanyIdAsync();

            var invoice = await _context.BillingInvoice
                .FirstOrDefaultAsync(i => i.BillingInvoiceID == id && i.CompanyID == companyId);

            if (invoice == null)
            {
                return NotFound();
            }

            invoice.PaymentStatus = "Paid";

            _context.BillingInvoice.Update(invoice);
            await _context.SaveChangesAsync();

            // SignalR: notify invoice paid
            await _hub.NotifyInvoicePaid(companyId, invoice.Amount ?? 0);
            await _hub.NotifyDashboardRefresh(companyId, "Finance");
            var paidUser = await _userManager.GetUserAsync(User);
            await _notificationService.NotifyCompanyActionAsync(
                paidUser?.Id ?? "", companyId, $"Invoice marked as paid: ₱{invoice.Amount:N2}", "Finance", "bi-receipt");

            return Ok(new { success = true, message = "Invoice marked as paid." });
        }

        // POST: Finance/DeleteInvoice/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteInvoice(int id)
        {
            var companyId = await GetCompanyIdAsync();

            var invoice = await _context.BillingInvoice
                .FirstOrDefaultAsync(i => i.BillingInvoiceID == id && i.CompanyID == companyId);

            if (invoice == null)
            {
                return NotFound();
            }

            _context.BillingInvoice.Remove(invoice);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Invoice deleted successfully." });
        }

        // GET: Finance/GetInvoiceDetail/{id}
        [HttpGet]
        public async Task<IActionResult> GetInvoiceDetail(int id)
        {
            var companyId = await GetCompanyIdAsync();

            var invoice = await _context.BillingInvoice
                .FirstOrDefaultAsync(i => i.BillingInvoiceID == id && i.CompanyID == companyId);

            if (invoice == null)
            {
                return NotFound();
            }

            var viewModel = new BillingInvoiceListViewModel
            {
                BillingInvoiceID = invoice.BillingInvoiceID,
                Amount = invoice.Amount ?? 0,
                InvoiceDate = invoice.InvoiceDate,
                DueDate = invoice.DueDate,
                PaymentStatus = invoice.PaymentStatus ?? "Pending"
            };

            return Json(viewModel);
        }

        // GET: Finance/CheckBudget?category=Raw%20Materials&amount=500&date=2026-03-10
        [HttpGet]
        public async Task<IActionResult> CheckBudget(string category, decimal amount, string? date)
        {
            var companyId = await GetCompanyIdAsync();
            var expenseDate = DateTime.TryParse(date, out var d) ? d : DateTime.UtcNow;
            var period = expenseDate.ToString("yyyy-MM");
            var monthStart = new DateTime(expenseDate.Year, expenseDate.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            var budget = await _context.Budget
                .FirstOrDefaultAsync(b => b.CompanyID == companyId && b.Period == period && b.Category == category);

            if (budget == null)
            {
                return Json(new BudgetCheckResult
                {
                    HasBudget = false,
                    IsOverLimit = false,
                    Category = category,
                    Period = period
                });
            }

            var spent = await _context.Expense
                .Where(e => e.CompanyID == companyId
                    && e.Category == category
                    && e.ExpenseDate.HasValue
                    && e.ExpenseDate.Value >= monthStart
                    && e.ExpenseDate.Value < monthEnd)
                .SumAsync(e => (decimal?)e.Amount ?? 0);

            var remaining = (budget.AllocatedAmount ?? 0) - spent;

            return Json(new BudgetCheckResult
            {
                HasBudget = true,
                IsOverLimit = amount > remaining,
                AllocatedAmount = budget.AllocatedAmount ?? 0,
                SpentAmount = spent,
                RemainingAmount = remaining,
                Category = category,
                Period = period
            });
        }

        // =====================================================
        // BUDGET ALLOTMENT PAGE
        // =====================================================

        // GET: Finance/BudgetAllotment
        [HttpGet]
        public async Task<IActionResult> BudgetAllotment()
        {
            var companyId = await GetCompanyIdAsync();

            var budgets = await _context.Budget
                .Where(b => b.CompanyID == companyId)
                .OrderByDescending(b => b.Period).ThenBy(b => b.Category)
                .ToListAsync();

            var allBudgetVMs = new List<BudgetListViewModel>();
            foreach (var b in budgets)
            {
                decimal spent = 0;
                int emergencyCount = 0;

                if (DateTime.TryParse(b.Period + "-01", out var budgetMonth))
                {
                    var bStart = new DateTime(budgetMonth.Year, budgetMonth.Month, 1);
                    var bEnd = bStart.AddMonths(1);

                    spent = await _context.Expense
                        .Where(e => e.CompanyID == companyId && e.Category == b.Category
                            && e.ExpenseDate.HasValue && e.ExpenseDate.Value >= bStart && e.ExpenseDate.Value < bEnd)
                        .SumAsync(e => (decimal?)e.Amount ?? 0);

                    emergencyCount = await _context.Expense
                        .CountAsync(e => e.CompanyID == companyId && e.Category == b.Category
                            && e.IsEmergency && e.ExpenseDate.HasValue
                            && e.ExpenseDate.Value >= bStart && e.ExpenseDate.Value < bEnd);
                }

                allBudgetVMs.Add(new BudgetListViewModel
                {
                    BudgetID = b.BudgetID,
                    Period = b.Period ?? "N/A",
                    Category = b.Category ?? "Other",
                    AllocatedAmount = b.AllocatedAmount ?? 0,
                    SpentAmount = spent,
                    RemainingAmount = (b.AllocatedAmount ?? 0) - spent,
                    IsOverBudget = spent > (b.AllocatedAmount ?? 0),
                    UtilizationPercentage = (b.AllocatedAmount ?? 0) > 0
                        ? Math.Min(100, (spent / (b.AllocatedAmount ?? 1)) * 100) : 0,
                    EmergencyCount = emergencyCount
                });
            }

            ViewBag.Budgets = allBudgetVMs;
            return View();
        }

        // =====================================================
        // PURCHASE RECORDS PAGE
        // =====================================================

        // GET: Finance/PurchaseRecords
        [HttpGet]
        public async Task<IActionResult> PurchaseRecords(string? category, bool? overBudgetOnly)
        {
            var companyId = await GetCompanyIdAsync();

            var query = _context.Expense
                .Include(e => e.Supplier)
                .Include(e => e.User)
                .Where(e => e.CompanyID == companyId);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(e => e.Category == category);

            if (overBudgetOnly == true)
                query = query.Where(e => e.IsEmergency);

            var expenses = await query
                .OrderByDescending(e => e.ExpenseDate)
                .Select(e => new ExpenseListViewModel
                {
                    ExpenseID = e.ExpenseID,
                    Amount = e.Amount ?? 0,
                    ExpenseDate = e.ExpenseDate,
                    SupplierName = e.Supplier != null ? (e.Supplier.SupplierName ?? "N/A") : "N/A",
                    Category = e.Category ?? "Other",
                    UserName = e.User.UserName ?? e.User.Email ?? "Unknown",
                    IsEmergency = e.IsEmergency,
                    EmergencyReason = e.EmergencyReason
                })
                .ToListAsync();

            // Category summary
            var categorySummaries = await _context.Expense
                .Where(e => e.CompanyID == companyId)
                .GroupBy(e => e.Category ?? "Other")
                .Select(g => new { Category = g.Key, Total = g.Sum(e => (decimal?)e.Amount ?? 0), Count = g.Count() })
                .ToListAsync();

            var overBudgetCount = await _context.Expense
                .CountAsync(e => e.CompanyID == companyId && e.IsEmergency);

            ViewBag.Expenses = expenses;
            ViewBag.CategorySummaries = categorySummaries;
            ViewBag.OverBudgetCount = overBudgetCount;
            ViewBag.CurrentCategory = category;
            ViewBag.ShowOverBudgetOnly = overBudgetOnly ?? false;
            return View();
        }

        // =====================================================
        // BUDGET STATUS API (accessible to ProductManager too)
        // =====================================================

        // GET: Finance/GetBudgetStatus?category=Raw%20Materials
        [HttpGet]
        [Authorize(Roles = "Admin,Finance,ProductManager")]
        public async Task<IActionResult> GetBudgetStatus(string category)
        {
            var companyId = await GetCompanyIdAsync();
            var now = DateTime.UtcNow;
            var period = now.ToString("yyyy-MM");
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            var budget = await _context.Budget
                .FirstOrDefaultAsync(b => b.CompanyID == companyId && b.Period == period && b.Category == category);

            if (budget == null)
            {
                return Json(new
                {
                    hasBudget = false,
                    category = category,
                    period = period
                });
            }

            var spent = await _context.Expense
                .Where(e => e.CompanyID == companyId && e.Category == category
                    && e.ExpenseDate.HasValue && e.ExpenseDate.Value >= monthStart && e.ExpenseDate.Value < monthEnd)
                .SumAsync(e => (decimal?)e.Amount ?? 0);

            var emergencyCount = await _context.Expense
                .CountAsync(e => e.CompanyID == companyId && e.Category == category
                    && e.IsEmergency && e.ExpenseDate.HasValue
                    && e.ExpenseDate.Value >= monthStart && e.ExpenseDate.Value < monthEnd);

            var allocated = budget.AllocatedAmount ?? 0;
            var remaining = allocated - spent;

            return Json(new
            {
                hasBudget = true,
                category = category,
                period = period,
                allocated = allocated,
                spent = spent,
                remaining = remaining,
                utilization = allocated > 0 ? Math.Min(100, (spent / allocated) * 100) : 0,
                isOverBudget = spent > allocated,
                emergencyCount = emergencyCount
            });
        }
    }
}
