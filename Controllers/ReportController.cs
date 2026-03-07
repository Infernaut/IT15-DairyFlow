using IT15_DairyFlow.Data;
using IT15_DairyFlow.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace IT15_DairyFlow.Controllers
{
    /// <summary>
    /// Generates PDF reports (invoices, production reports, inspection certificates)
    /// using QuestPDF Community edition.
    /// </summary>
    [Authorize]
    [Route("[controller]/[action]")]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<int> GetCompanyIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.CompanyID ?? 1;
        }

        private async Task<string> GetCompanyNameAsync(int companyId)
        {
            var company = await _context.Company.FindAsync(companyId);
            return company?.CompanyName ?? "DairyFlow";
        }

        // ───────────────────────────────────────────────────────────
        //  INVOICE PDF
        // ───────────────────────────────────────────────────────────

        [Authorize(Roles = "Admin,Finance")]
        public async Task<IActionResult> Invoice(int id)
        {
            var companyId = await GetCompanyIdAsync();
            var invoice = await _context.BillingInvoice
                .Include(i => i.Company)
                .FirstOrDefaultAsync(i => i.BillingInvoiceID == id && i.CompanyID == companyId);

            if (invoice == null) return NotFound();

            var companyName = invoice.Company?.CompanyName ?? "DairyFlow";

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(companyName).FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                            col.Item().Text("Tax Invoice").FontSize(14).FontColor(Colors.Grey.Medium);
                        });
                        row.ConstantItem(120).AlignRight().Column(col =>
                        {
                            col.Item().Text($"INV-{invoice.BillingInvoiceID:D5}").FontSize(14).Bold();
                            col.Item().Text(invoice.InvoiceDate?.ToString("MMM dd, yyyy") ?? "").FontColor(Colors.Grey.Medium);
                        });
                    });

                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        // Invoice Details Table
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn();
                                cols.RelativeColumn();
                            });

                            table.Cell().Padding(8).Background(Colors.Blue.Lighten4)
                                .Text("Invoice Date").Bold();
                            table.Cell().Padding(8).Background(Colors.Blue.Lighten4)
                                .Text(invoice.InvoiceDate?.ToString("MMMM dd, yyyy") ?? "N/A");

                            table.Cell().Padding(8)
                                .Text("Due Date").Bold();
                            table.Cell().Padding(8)
                                .Text(invoice.DueDate?.ToString("MMMM dd, yyyy") ?? "N/A");

                            table.Cell().Padding(8).Background(Colors.Blue.Lighten4)
                                .Text("Payment Status").Bold();
                            table.Cell().Padding(8).Background(Colors.Blue.Lighten4)
                                .Text(invoice.PaymentStatus ?? "Pending");

                            table.Cell().Padding(8)
                                .Text("Amount").Bold();
                            table.Cell().Padding(8)
                                .Text($"₱{invoice.Amount:N2}").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                        });

                        col.Item().PaddingTop(30).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        col.Item().PaddingTop(15).AlignRight().Text($"Total Due: ₱{invoice.Amount:N2}")
                            .FontSize(18).Bold().FontColor(Colors.Blue.Medium);
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Generated by DairyFlow ERP on ").FontColor(Colors.Grey.Medium);
                        text.Span(DateTime.Now.ToString("MMM dd, yyyy HH:mm")).FontColor(Colors.Grey.Medium);
                    });
                });
            });

            var bytes = pdf.GeneratePdf();
            return File(bytes, "application/pdf", $"Invoice-{invoice.BillingInvoiceID}.pdf");
        }

        // ───────────────────────────────────────────────────────────
        //  PRODUCTION REPORT PDF
        // ───────────────────────────────────────────────────────────

        [Authorize(Roles = "Admin,ProductManager")]
        public async Task<IActionResult> ProductionReport()
        {
            var companyId = await GetCompanyIdAsync();
            var companyName = await GetCompanyNameAsync(companyId);

            var batches = await _context.ProductionBatch
                .Include(b => b.Product)
                .Include(b => b.Equipment)
                .Where(b => b.CompanyID == companyId)
                .OrderByDescending(b => b.StartDate)
                .Take(50)
                .ToListAsync();

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().Text(companyName).FontSize(18).Bold().FontColor(Colors.Blue.Medium);
                        col.Item().Text("Production Batch Report").FontSize(12).FontColor(Colors.Grey.Medium);
                        col.Item().Text($"Generated: {DateTime.Now:MMM dd, yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Medium);
                        col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2); // Batch Code
                            cols.RelativeColumn(3); // Product
                            cols.RelativeColumn(2); // Equipment
                            cols.RelativeColumn(1.5f); // Status
                            cols.RelativeColumn(1); // Qty
                            cols.RelativeColumn(2); // Start
                            cols.RelativeColumn(2); // End
                        });

                        // Header
                        foreach (var header in new[] { "Batch Code", "Product", "Equipment", "Status", "Qty", "Start Date", "End Date" })
                        {
                            table.Cell().Background(Colors.Blue.Medium).Padding(6)
                                .Text(header).FontColor(Colors.White).Bold().FontSize(9);
                        }

                        // Data
                        var alt = false;
                        foreach (var b in batches)
                        {
                            var bg = alt ? Colors.Blue.Lighten5 : Colors.White;
                            table.Cell().Background(bg).Padding(5).Text(b.BatchCode ?? "");
                            table.Cell().Background(bg).Padding(5).Text(b.Product?.ProductName ?? "");
                            table.Cell().Background(bg).Padding(5).Text(b.Equipment?.EquipmentName ?? "");
                            table.Cell().Background(bg).Padding(5).Text(b.Status ?? "");
                            table.Cell().Background(bg).Padding(5).Text(b.Quantity.ToString());
                            table.Cell().Background(bg).Padding(5).Text(b.StartDate?.ToString("yyyy-MM-dd") ?? "");
                            table.Cell().Background(bg).Padding(5).Text(b.EndDate?.ToString("yyyy-MM-dd") ?? "");
                            alt = !alt;
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
                });
            });

            var bytes = pdf.GeneratePdf();
            return File(bytes, "application/pdf", "ProductionReport.pdf");
        }

        // ───────────────────────────────────────────────────────────
        //  QUALITY INSPECTION CERTIFICATE PDF
        // ───────────────────────────────────────────────────────────

        [Authorize(Roles = "Admin,QualityChecker")]
        public async Task<IActionResult> InspectionCertificate(int id)
        {
            var companyId = await GetCompanyIdAsync();
            var inspection = await _context.QualityInspection
                .Include(q => q.ProductionBatch)
                    .ThenInclude(b => b.Product)
                .Include(q => q.User)
                .FirstOrDefaultAsync(q => q.QualityInspectionID == id && q.CompanyID == companyId);

            if (inspection == null) return NotFound();

            var companyName = await GetCompanyNameAsync(companyId);
            var isPassed = inspection.Result?.ToLower() == "pass";

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text(companyName).FontSize(22).Bold().FontColor(Colors.Blue.Medium);
                        col.Item().AlignCenter().Text("Quality Inspection Certificate").FontSize(14).FontColor(Colors.Grey.Medium);
                        col.Item().PaddingTop(10).LineHorizontal(2).LineColor(Colors.Blue.Medium);
                    });

                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        // Batch Info
                        col.Item().PaddingBottom(10).Text("Batch Information").FontSize(13).Bold();

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn();
                                cols.RelativeColumn(2);
                            });

                            AddInfoRow(table, "Batch Code", inspection.ProductionBatch?.BatchCode ?? "N/A");
                            AddInfoRow(table, "Product", inspection.ProductionBatch?.Product?.ProductName ?? "N/A");
                            AddInfoRow(table, "Quantity", inspection.ProductionBatch?.Quantity.ToString() ?? "N/A");
                            AddInfoRow(table, "Inspection Type", inspection.Type ?? "General");
                            AddInfoRow(table, "Inspection Date", inspection.InspectionDate?.ToString("MMMM dd, yyyy") ?? "N/A");
                            AddInfoRow(table, "Inspector", inspection.User?.UserName ?? "N/A");
                        });

                        col.Item().PaddingVertical(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Result
                        col.Item().PaddingBottom(10).Text("Inspection Result").FontSize(13).Bold();

                        col.Item().Padding(15).Background(isPassed ? Colors.Green.Lighten4 : Colors.Red.Lighten4)
                            .AlignCenter()
                            .Text(isPassed ? "PASSED" : (inspection.Result?.ToUpper() ?? "PENDING"))
                            .FontSize(24).Bold()
                            .FontColor(isPassed ? Colors.Green.Medium : Colors.Red.Medium);

                        if (!string.IsNullOrEmpty(inspection.Notes))
                        {
                            col.Item().PaddingTop(15).Text("Notes").FontSize(13).Bold();
                            col.Item().PaddingTop(5).Text(inspection.Notes);
                        }

                        // Test results
                        if (inspection.Temperature.HasValue || inspection.PHLevel.HasValue)
                        {
                            col.Item().PaddingTop(15).Text("Test Details").FontSize(13).Bold();
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn();
                                    cols.RelativeColumn(2);
                                });

                                if (inspection.Temperature.HasValue)
                                    AddInfoRow(table, "Temperature", $"{inspection.Temperature}°C");
                                if (inspection.PHLevel.HasValue)
                                    AddInfoRow(table, "pH Level", inspection.PHLevel.ToString()!);
                                if (inspection.FatContent.HasValue)
                                    AddInfoRow(table, "Fat Content", $"{inspection.FatContent}%");
                                if (inspection.ProteinContent.HasValue)
                                    AddInfoRow(table, "Protein Content", $"{inspection.ProteinContent}%");
                                if (inspection.MoistureContent.HasValue)
                                    AddInfoRow(table, "Moisture Content", $"{inspection.MoistureContent}%");
                            });
                        }
                    });

                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        col.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Text($"Certificate ID: QIC-{inspection.QualityInspectionID:D5}").FontSize(8).FontColor(Colors.Grey.Medium);
                            row.RelativeItem().AlignRight().Text($"Generated: {DateTime.Now:MMM dd, yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Medium);
                        });
                    });
                });
            });

            var bytes = pdf.GeneratePdf();
            return File(bytes, "application/pdf", $"InspectionCertificate-{inspection.QualityInspectionID}.pdf");
        }

        // ───────────────────────────────────────────────────────────
        //  FINANCIAL SUMMARY REPORT PDF
        // ───────────────────────────────────────────────────────────

        [Authorize(Roles = "Admin,Finance")]
        public async Task<IActionResult> FinancialSummary()
        {
            var companyId = await GetCompanyIdAsync();
            var companyName = await GetCompanyNameAsync(companyId);

            var expenses = await _context.Expense
                .Include(e => e.Supplier)
                .Where(e => e.CompanyID == companyId)
                .OrderByDescending(e => e.ExpenseDate)
                .Take(50)
                .ToListAsync();

            var invoices = await _context.BillingInvoice
                .Where(i => i.CompanyID == companyId)
                .OrderByDescending(i => i.InvoiceDate)
                .Take(50)
                .ToListAsync();

            var totalRevenue = invoices.Where(i => i.PaymentStatus == "Paid").Sum(i => i.Amount ?? 0);
            var totalExpenses = expenses.Sum(e => e.Amount ?? 0);

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(35);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Text(companyName).FontSize(18).Bold().FontColor(Colors.Blue.Medium);
                        col.Item().Text("Financial Summary Report").FontSize(12).FontColor(Colors.Grey.Medium);
                        col.Item().Text($"Generated: {DateTime.Now:MMM dd, yyyy}").FontSize(8).FontColor(Colors.Grey.Medium);
                        col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        // Summary KPIs
                        col.Item().PaddingBottom(10).Row(row =>
                        {
                            row.RelativeItem().Padding(10).Background(Colors.Green.Lighten4).Column(c =>
                            {
                                c.Item().Text("Total Revenue").FontSize(9).FontColor(Colors.Grey.Medium);
                                c.Item().Text($"₱{totalRevenue:N2}").FontSize(16).Bold().FontColor(Colors.Green.Medium);
                            });
                            row.ConstantItem(10);
                            row.RelativeItem().Padding(10).Background(Colors.Red.Lighten4).Column(c =>
                            {
                                c.Item().Text("Total Expenses").FontSize(9).FontColor(Colors.Grey.Medium);
                                c.Item().Text($"₱{totalExpenses:N2}").FontSize(16).Bold().FontColor(Colors.Red.Medium);
                            });
                            row.ConstantItem(10);
                            var net = totalRevenue - totalExpenses;
                            row.RelativeItem().Padding(10).Background(net >= 0 ? Colors.Blue.Lighten4 : Colors.Orange.Lighten4).Column(c =>
                            {
                                c.Item().Text("Net Profit").FontSize(9).FontColor(Colors.Grey.Medium);
                                c.Item().Text($"₱{net:N2}").FontSize(16).Bold().FontColor(net >= 0 ? Colors.Blue.Medium : Colors.Orange.Medium);
                            });
                        });

                        // Recent Expenses Table
                        col.Item().PaddingTop(15).Text("Recent Expenses").FontSize(12).Bold();
                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(2); // Date
                                cols.RelativeColumn(2); // Supplier
                                cols.RelativeColumn(2); // Amount
                            });

                            foreach (var h in new[] { "Date", "Supplier", "Amount" })
                                table.Cell().Background(Colors.Blue.Medium).Padding(5).Text(h).FontColor(Colors.White).Bold();

                            var alt = false;
                            foreach (var e in expenses.Take(20))
                            {
                                var bg = alt ? Colors.Blue.Lighten5 : Colors.White;
                                table.Cell().Background(bg).Padding(4).Text(e.ExpenseDate?.ToString("yyyy-MM-dd") ?? "");
                                table.Cell().Background(bg).Padding(4).Text(e.Supplier?.SupplierName ?? "N/A");
                                table.Cell().Background(bg).Padding(4).Text($"₱{e.Amount:N2}");
                                alt = !alt;
                            }
                        });

                        // Recent Invoices Table
                        col.Item().PaddingTop(20).Text("Recent Invoices").FontSize(12).Bold();
                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(1.5f);
                            });

                            foreach (var h in new[] { "Invoice Date", "Due Date", "Amount", "Status" })
                                table.Cell().Background(Colors.Blue.Medium).Padding(5).Text(h).FontColor(Colors.White).Bold();

                            var alt = false;
                            foreach (var inv in invoices.Take(20))
                            {
                                var bg = alt ? Colors.Blue.Lighten5 : Colors.White;
                                table.Cell().Background(bg).Padding(4).Text(inv.InvoiceDate?.ToString("yyyy-MM-dd") ?? "");
                                table.Cell().Background(bg).Padding(4).Text(inv.DueDate?.ToString("yyyy-MM-dd") ?? "");
                                table.Cell().Background(bg).Padding(4).Text($"₱{inv.Amount:N2}");
                                table.Cell().Background(bg).Padding(4).Text(inv.PaymentStatus ?? "");
                                alt = !alt;
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
                });
            });

            var bytes = pdf.GeneratePdf();
            return File(bytes, "application/pdf", "FinancialSummary.pdf");
        }

        // ── Helper ────────────────────────────────────────────────

        private static void AddInfoRow(TableDescriptor table, string label, string value)
        {
            table.Cell().Padding(6).Background(Colors.Grey.Lighten4).Text(label).Bold();
            table.Cell().Padding(6).Text(value);
        }
    }
}
