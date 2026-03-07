using ClosedXML.Excel;
using IT15_DairyFlow.Data;
using IT15_DairyFlow.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IT15_DairyFlow.Controllers
{
    /// <summary>
    /// Provides Excel export endpoints for all DairyFlow modules using ClosedXML.
    /// Each endpoint returns a .xlsx file download.
    /// </summary>
    [Authorize]
    [Route("[controller]/[action]")]
    public class ExportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExportController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<int> GetCompanyIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.CompanyID ?? 1;
        }

        // ───────────────────────────────────────────────────────────
        //  PRODUCTION EXPORTS
        // ───────────────────────────────────────────────────────────

        [Authorize(Roles = "Admin,ProductManager")]
        public async Task<IActionResult> ProductionBatches()
        {
            var companyId = await GetCompanyIdAsync();
            var batches = await _context.ProductionBatch
                .Include(b => b.Product)
                .Include(b => b.Equipment)
                .Include(b => b.User)
                .Where(b => b.CompanyID == companyId)
                .OrderByDescending(b => b.StartDate)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Production Batches");

            // Header
            ws.Cell(1, 1).Value = "Batch Code";
            ws.Cell(1, 2).Value = "Product";
            ws.Cell(1, 3).Value = "Equipment";
            ws.Cell(1, 4).Value = "Status";
            ws.Cell(1, 5).Value = "Quantity";
            ws.Cell(1, 6).Value = "Start Date";
            ws.Cell(1, 7).Value = "End Date";
            ws.Cell(1, 8).Value = "Created By";
            StyleHeader(ws, 8);

            for (int i = 0; i < batches.Count; i++)
            {
                var b = batches[i];
                var row = i + 2;
                ws.Cell(row, 1).Value = b.BatchCode ?? "";
                ws.Cell(row, 2).Value = b.Product?.ProductName ?? "";
                ws.Cell(row, 3).Value = b.Equipment?.EquipmentName ?? "";
                ws.Cell(row, 4).Value = b.Status ?? "";
                ws.Cell(row, 5).Value = b.Quantity;
                ws.Cell(row, 6).Value = b.StartDate?.ToString("yyyy-MM-dd") ?? "";
                ws.Cell(row, 7).Value = b.EndDate?.ToString("yyyy-MM-dd") ?? "";
                ws.Cell(row, 8).Value = b.User?.UserName ?? "";
            }

            ws.Columns().AdjustToContents();
            return WorkbookToFile(workbook, "ProductionBatches.xlsx");
        }

        [Authorize(Roles = "Admin,ProductManager")]
        public async Task<IActionResult> ProductionCosts()
        {
            var companyId = await GetCompanyIdAsync();
            var costs = await _context.ProductionCost
                .Include(c => c.ProductionBatch)
                    .ThenInclude(b => b.Product)
                .Where(c => c.CompanyID == companyId)
                .OrderByDescending(c => c.ProductionCostID)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Production Costs");

            ws.Cell(1, 1).Value = "Batch Code";
            ws.Cell(1, 2).Value = "Product";
            ws.Cell(1, 3).Value = "Total Cost";
            StyleHeader(ws, 3);

            for (int i = 0; i < costs.Count; i++)
            {
                var c = costs[i];
                var row = i + 2;
                ws.Cell(row, 1).Value = c.ProductionBatch?.BatchCode ?? "";
                ws.Cell(row, 2).Value = c.ProductionBatch?.Product?.ProductName ?? "";
                ws.Cell(row, 3).Value = c.TotalCost;
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
            }

            ws.Columns().AdjustToContents();
            return WorkbookToFile(workbook, "ProductionCosts.xlsx");
        }

        // ───────────────────────────────────────────────────────────
        //  INVENTORY EXPORTS
        // ───────────────────────────────────────────────────────────

        [Authorize(Roles = "Admin,ProductManager,QualityChecker")]
        public async Task<IActionResult> FinishedGoods()
        {
            var companyId = await GetCompanyIdAsync();
            var items = await _context.Inventory
                .Include(i => i.Product)
                .Where(i => i.CompanyID == companyId)
                .OrderBy(i => i.Product!.ProductName)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Finished Goods");

            ws.Cell(1, 1).Value = "Product";
            ws.Cell(1, 2).Value = "Quantity";
            ws.Cell(1, 3).Value = "Expiry Date";
            StyleHeader(ws, 3);

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var row = i + 2;
                ws.Cell(row, 1).Value = item.Product?.ProductName ?? "";
                ws.Cell(row, 2).Value = item.Quantity ?? 0;
                ws.Cell(row, 3).Value = item.Expiry?.ToString("yyyy-MM-dd") ?? "N/A";
            }

            ws.Columns().AdjustToContents();
            return WorkbookToFile(workbook, "FinishedGoods.xlsx");
        }

        [Authorize(Roles = "Admin,ProductManager")]
        public async Task<IActionResult> RawMaterials()
        {
            var companyId = await GetCompanyIdAsync();
            var materials = await _context.RawMaterial
                .Include(r => r.Supplier)
                .Where(r => r.CompanyID == companyId)
                .OrderBy(r => r.MaterialName)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Raw Materials");

            ws.Cell(1, 1).Value = "Material Name";
            ws.Cell(1, 2).Value = "Unit";
            ws.Cell(1, 3).Value = "Unit Cost";
            ws.Cell(1, 4).Value = "Current Stock";
            ws.Cell(1, 5).Value = "Minimum Stock";
            ws.Cell(1, 6).Value = "Supplier";
            StyleHeader(ws, 6);

            for (int i = 0; i < materials.Count; i++)
            {
                var m = materials[i];
                var row = i + 2;
                ws.Cell(row, 1).Value = m.MaterialName ?? "";
                ws.Cell(row, 2).Value = m.Unit ?? "";
                ws.Cell(row, 3).Value = m.UnitCost;
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 4).Value = m.CurrentStock ?? 0;
                ws.Cell(row, 5).Value = m.MinimumStock ?? 0;
                ws.Cell(row, 6).Value = m.Supplier?.SupplierName ?? "N/A";
            }

            ws.Columns().AdjustToContents();
            return WorkbookToFile(workbook, "RawMaterials.xlsx");
        }

        // ───────────────────────────────────────────────────────────
        //  QUALITY EXPORTS
        // ───────────────────────────────────────────────────────────

        [Authorize(Roles = "Admin,QualityChecker")]
        public async Task<IActionResult> Inspections()
        {
            var companyId = await GetCompanyIdAsync();
            var inspections = await _context.QualityInspection
                .Include(q => q.ProductionBatch)
                    .ThenInclude(b => b.Product)
                .Include(q => q.User)
                .Where(q => q.CompanyID == companyId)
                .OrderByDescending(q => q.InspectionDate)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Quality Inspections");

            ws.Cell(1, 1).Value = "Batch Code";
            ws.Cell(1, 2).Value = "Product";
            ws.Cell(1, 3).Value = "Type";
            ws.Cell(1, 4).Value = "Result";
            ws.Cell(1, 5).Value = "Status";
            ws.Cell(1, 6).Value = "Inspector";
            ws.Cell(1, 7).Value = "Inspection Date";
            ws.Cell(1, 8).Value = "Notes";
            StyleHeader(ws, 8);

            for (int i = 0; i < inspections.Count; i++)
            {
                var q = inspections[i];
                var row = i + 2;
                ws.Cell(row, 1).Value = q.ProductionBatch?.BatchCode ?? "";
                ws.Cell(row, 2).Value = q.ProductionBatch?.Product?.ProductName ?? "";
                ws.Cell(row, 3).Value = q.Type ?? "";
                ws.Cell(row, 4).Value = q.Result ?? "";
                ws.Cell(row, 5).Value = q.Status ?? "";
                ws.Cell(row, 6).Value = q.User?.UserName ?? "";
                ws.Cell(row, 7).Value = q.InspectionDate?.ToString("yyyy-MM-dd") ?? "";
                ws.Cell(row, 8).Value = q.Notes ?? "";
            }

            ws.Columns().AdjustToContents();
            return WorkbookToFile(workbook, "QualityInspections.xlsx");
        }

        [Authorize(Roles = "Admin,QualityChecker")]
        public async Task<IActionResult> NonConformances()
        {
            var companyId = await GetCompanyIdAsync();
            var ncrs = await _context.NonConformance
                .Include(n => n.ProductionBatch)
                .Where(n => n.CompanyID == companyId)
                .OrderByDescending(n => n.ReportedDate)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Non-Conformances");

            ws.Cell(1, 1).Value = "NCR Number";
            ws.Cell(1, 2).Value = "Batch Code";
            ws.Cell(1, 3).Value = "Category";
            ws.Cell(1, 4).Value = "Severity";
            ws.Cell(1, 5).Value = "Status";
            ws.Cell(1, 6).Value = "Reported Date";
            ws.Cell(1, 7).Value = "Description";
            ws.Cell(1, 8).Value = "Root Cause";
            ws.Cell(1, 9).Value = "Corrective Action";
            StyleHeader(ws, 9);

            for (int i = 0; i < ncrs.Count; i++)
            {
                var n = ncrs[i];
                var row = i + 2;
                ws.Cell(row, 1).Value = n.NCRNumber ?? "";
                ws.Cell(row, 2).Value = n.ProductionBatch?.BatchCode ?? "";
                ws.Cell(row, 3).Value = n.Category ?? "";
                ws.Cell(row, 4).Value = n.Severity ?? "";
                ws.Cell(row, 5).Value = n.Status ?? "";
                ws.Cell(row, 6).Value = n.ReportedDate.ToString("yyyy-MM-dd");
                ws.Cell(row, 7).Value = n.Description ?? "";
                ws.Cell(row, 8).Value = n.RootCause ?? "";
                ws.Cell(row, 9).Value = n.CorrectiveAction ?? "";
            }

            ws.Columns().AdjustToContents();
            return WorkbookToFile(workbook, "NonConformances.xlsx");
        }

        // ───────────────────────────────────────────────────────────
        //  FINANCE EXPORTS
        // ───────────────────────────────────────────────────────────

        [Authorize(Roles = "Admin,Finance")]
        public async Task<IActionResult> Expenses()
        {
            var companyId = await GetCompanyIdAsync();
            var expenses = await _context.Expense
                .Include(e => e.Supplier)
                .Include(e => e.User)
                .Where(e => e.CompanyID == companyId)
                .OrderByDescending(e => e.ExpenseDate)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Expenses");

            ws.Cell(1, 1).Value = "Amount";
            ws.Cell(1, 2).Value = "Date";
            ws.Cell(1, 3).Value = "Supplier";
            ws.Cell(1, 4).Value = "Created By";
            StyleHeader(ws, 4);

            for (int i = 0; i < expenses.Count; i++)
            {
                var e = expenses[i];
                var row = i + 2;
                ws.Cell(row, 1).Value = e.Amount ?? 0;
                ws.Cell(row, 1).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 2).Value = e.ExpenseDate?.ToString("yyyy-MM-dd") ?? "";
                ws.Cell(row, 3).Value = e.Supplier?.SupplierName ?? "N/A";
                ws.Cell(row, 4).Value = e.User?.UserName ?? "";
            }

            ws.Columns().AdjustToContents();
            return WorkbookToFile(workbook, "Expenses.xlsx");
        }

        [Authorize(Roles = "Admin,Finance")]
        public async Task<IActionResult> Budgets()
        {
            var companyId = await GetCompanyIdAsync();
            var budgets = await _context.Budget
                .Where(b => b.CompanyID == companyId)
                .OrderByDescending(b => b.BudgetID)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Budgets");

            ws.Cell(1, 1).Value = "Period";
            ws.Cell(1, 2).Value = "Allocated Amount";
            StyleHeader(ws, 2);

            for (int i = 0; i < budgets.Count; i++)
            {
                var b = budgets[i];
                var row = i + 2;
                ws.Cell(row, 1).Value = b.Period ?? "";
                ws.Cell(row, 2).Value = b.AllocatedAmount;
                ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
            }

            ws.Columns().AdjustToContents();
            return WorkbookToFile(workbook, "Budgets.xlsx");
        }

        [Authorize(Roles = "Admin,Finance")]
        public async Task<IActionResult> Invoices()
        {
            var companyId = await GetCompanyIdAsync();
            var invoices = await _context.BillingInvoice
                .Where(i => i.CompanyID == companyId)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Invoices");

            ws.Cell(1, 1).Value = "Invoice Date";
            ws.Cell(1, 2).Value = "Due Date";
            ws.Cell(1, 3).Value = "Amount";
            ws.Cell(1, 4).Value = "Payment Status";
            StyleHeader(ws, 4);

            for (int i = 0; i < invoices.Count; i++)
            {
                var inv = invoices[i];
                var row = i + 2;
                ws.Cell(row, 1).Value = inv.InvoiceDate?.ToString("yyyy-MM-dd") ?? "";
                ws.Cell(row, 2).Value = inv.DueDate?.ToString("yyyy-MM-dd") ?? "";
                ws.Cell(row, 3).Value = inv.Amount ?? 0;
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 4).Value = inv.PaymentStatus ?? "";
            }

            ws.Columns().AdjustToContents();
            return WorkbookToFile(workbook, "Invoices.xlsx");
        }

        // ───────────────────────────────────────────────────────────
        //  HELPERS
        // ───────────────────────────────────────────────────────────

        private static void StyleHeader(IXLWorksheet ws, int colCount)
        {
            var headerRange = ws.Range(1, 1, 1, colCount);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#0d6efd");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        private FileContentResult WorkbookToFile(XLWorkbook workbook, string fileName)
        {
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;
            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}
