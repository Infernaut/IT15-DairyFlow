using IT15_DairyFlow.Data;
using IT15_DairyFlow.Models;
using IT15_DairyFlow.Models.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IT15_DairyFlow.Controllers
{
    [Authorize]
    public class FinanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FinanceController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
                    UserName = e.User.UserName ?? e.User.Email ?? "Unknown"
                })
                .ToListAsync();

            // Get budgets
            var budgets = await _context.Budget
                .Where(b => b.CompanyID == companyId)
                .OrderByDescending(b => b.BudgetID)
                .Select(b => new BudgetListViewModel
                {
                    BudgetID = b.BudgetID,
                    Period = b.Period ?? "N/A",
                    AllocatedAmount = b.AllocatedAmount ?? 0
                })
                .ToListAsync();

            // Calculate spent amount for each budget
            var totalExpenses = await _context.Expense
                .Where(e => e.CompanyID == companyId)
                .SumAsync(e => (decimal?)e.Amount ?? 0);

            foreach (var budget in budgets)
            {
                // Distribute expenses proportionally across budgets
                budget.SpentAmount = budgets.Count > 0 ? totalExpenses / budgets.Count : 0;
                budget.RemainingAmount = Math.Max(0, budget.AllocatedAmount - budget.SpentAmount);
                budget.UtilizationPercentage = budget.AllocatedAmount > 0 
                    ? Math.Min(100, (budget.SpentAmount / budget.AllocatedAmount) * 100) 
                    : 0;
            }

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
                Suppliers = suppliers
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

            var expense = new Expense
            {
                CompanyID = companyId,
                UserID = user.Id,
                Amount = model.Amount,
                ExpenseDate = model.ExpenseDate ?? DateTime.UtcNow,
                SupplierID = model.SupplierID
            };

            _context.Expense.Add(expense);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Expense recorded successfully." });
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
                UserID = expense.UserID
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

            // Check if budget for the period already exists
            var existingBudget = await _context.Budget
                .FirstOrDefaultAsync(b => b.CompanyID == companyId && b.Period == model.Period);

            if (existingBudget != null)
            {
                return BadRequest("A budget for this period already exists.");
            }

            var budget = new Budget
            {
                CompanyID = companyId,
                Period = model.Period,
                AllocatedAmount = model.AllocatedAmount
            };

            _context.Budget.Add(budget);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Budget created successfully." });
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
    }
}
