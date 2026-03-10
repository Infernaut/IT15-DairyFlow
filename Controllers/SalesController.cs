using IT15_DairyFlow.Data;
using IT15_DairyFlow.Hubs;
using IT15_DairyFlow.Models;
using IT15_DairyFlow.Models.Sales;
using IT15_DairyFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace IT15_DairyFlow.Controllers
{
    [Authorize(Roles = "Admin,ProductManager,Finance")]
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<DairyFlowHub> _hub;
        private readonly NotificationService _notificationService;
        private readonly PayMongoService _payMongoService;

        public SalesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IHubContext<DairyFlowHub> hub,
            NotificationService notificationService,
            PayMongoService payMongoService)
        {
            _context = context;
            _userManager = userManager;
            _hub = hub;
            _notificationService = notificationService;
            _payMongoService = payMongoService;
        }

        private async Task<int> GetCompanyIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.CompanyID ?? 1;
        }

        private string GenerateInvoiceNumber()
        {
            return $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        }

        // ── Sales Index (ProductManager & Admin view) ────────────────────

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var companyId = await GetCompanyIdAsync();

            var sales = await _context.Sale
                .Include(s => s.Product)
                .Include(s => s.CreatedByUser)
                .Where(s => s.CompanyID == companyId)
                .OrderByDescending(s => s.SaleDate)
                .Select(s => new SaleListViewModel
                {
                    SaleID = s.SaleID,
                    InvoiceNumber = s.InvoiceNumber,
                    ProductName = s.Product.ProductName ?? "Unknown",
                    BuyerName = s.BuyerName,
                    BuyerEmail = s.BuyerEmail,
                    BuyerPhone = s.BuyerPhone,
                    Quantity = s.Quantity,
                    UnitPrice = s.UnitPrice,
                    TotalAmount = s.TotalAmount,
                    SaleDate = s.SaleDate,
                    PaymentStatus = s.PaymentStatus,
                    PaymentMethod = s.PaymentMethod,
                    CreatedByName = s.CreatedByUser.UserName ?? s.CreatedByUser.Email ?? "Unknown",
                    Notes = s.Notes
                })
                .ToListAsync();

            var availableProducts = await _context.Inventory
                .Include(i => i.Product)
                .Where(i => i.CompanyID == companyId && (i.Quantity ?? 0) > 0)
                .Where(i => !i.Expiry.HasValue || i.Expiry.Value.Date > DateTime.UtcNow.Date)
                .OrderBy(i => i.Product.ProductName)
                .Select(i => new InventoryProductLookupViewModel
                {
                    InventoryID = i.InventoryID,
                    ProductID = i.ProductID,
                    ProductName = i.Product.ProductName ?? "Unknown",
                    AvailableQuantity = i.Quantity ?? 0
                })
                .ToListAsync();

            var summary = new SalesSummaryViewModel
            {
                TotalSales = sales.Count,
                TotalRevenue = sales.Where(s => s.PaymentStatus == "Paid").Sum(s => s.TotalAmount),
                PendingPayments = sales.Where(s => s.PaymentStatus == "Pending").Sum(s => s.TotalAmount),
                PaidCount = sales.Count(s => s.PaymentStatus == "Paid"),
                PendingCount = sales.Count(s => s.PaymentStatus == "Pending")
            };

            var viewModel = new SalesPageViewModel
            {
                Sales = sales,
                AvailableProducts = availableProducts,
                Summary = summary
            };

            return View(viewModel);
        }

        // ── Create Sale (ProductManager) ─────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProductManager")]
        public async Task<IActionResult> CreateSale([FromBody] CreateSaleViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var companyId = await GetCompanyIdAsync();

            // Validate inventory exists and has sufficient stock
            var inventory = await _context.Inventory
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.InventoryID == model.InventoryID && i.CompanyID == companyId);

            if (inventory == null)
                return NotFound(new { success = false, message = "Product inventory not found." });

            if ((inventory.Quantity ?? 0) < model.Quantity)
                return BadRequest(new { success = false, message = $"Insufficient stock. Available: {inventory.Quantity ?? 0}" });

            var sale = new Sale
            {
                CompanyID = companyId,
                InventoryID = model.InventoryID,
                ProductID = inventory.ProductID,
                BuyerName = model.BuyerName,
                BuyerEmail = model.BuyerEmail,
                BuyerPhone = model.BuyerPhone,
                Quantity = model.Quantity,
                UnitPrice = model.UnitPrice,
                TotalAmount = model.Quantity * model.UnitPrice,
                InvoiceNumber = GenerateInvoiceNumber(),
                SaleDate = DateTime.UtcNow,
                PaymentStatus = "Pending",
                CreatedByUserID = user.Id,
                Notes = model.Notes
            };

            _context.Sale.Add(sale);
            await _context.SaveChangesAsync();

            // SignalR notification
            await _hub.NotifySaleCreated(companyId, sale.InvoiceNumber, inventory.Product?.ProductName ?? "Unknown", user.UserName ?? "");
            await _hub.NotifyDashboardRefresh(companyId, "Sales");
            await _notificationService.NotifyCompanyActionAsync(
                user.Id, companyId, $"New sale created: {sale.InvoiceNumber} — ₱{sale.TotalAmount:N2}", "Sales", "bi-cart-check");

            return Ok(new { success = true, message = "Sale created successfully.", saleId = sale.SaleID, invoiceNumber = sale.InvoiceNumber });
        }

        // ── Process Cash Payment (Finance) ───────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Finance")]
        public async Task<IActionResult> ProcessCashPayment([FromBody] ProcessCashPaymentViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var companyId = await GetCompanyIdAsync();

            var sale = await _context.Sale
                .Include(s => s.Inventory)
                .FirstOrDefaultAsync(s => s.SaleID == model.SaleID && s.CompanyID == companyId);

            if (sale == null)
                return NotFound(new { success = false, message = "Sale not found." });

            if (sale.PaymentStatus == "Paid")
                return BadRequest(new { success = false, message = "This sale has already been paid." });

            // Update sale payment
            sale.PaymentStatus = "Paid";
            sale.PaymentMethod = "Cash";

            // Create transaction record
            var transaction = new SaleTransaction
            {
                SaleID = sale.SaleID,
                CompanyID = companyId,
                Amount = sale.TotalAmount,
                PaymentMethod = "Cash",
                PaymentDate = DateTime.UtcNow,
                ProcessedByUserID = user.Id,
                ReferenceNumber = model.ReferenceNumber,
                Notes = model.Notes
            };

            _context.SaleTransaction.Add(transaction);

            // Deduct inventory
            var inventory = await _context.Inventory.FindAsync(sale.InventoryID);
            if (inventory != null)
            {
                inventory.Quantity = Math.Max(0, (inventory.Quantity ?? 0) - sale.Quantity);
            }

            await _context.SaveChangesAsync();

            // Notifications
            await _hub.NotifyPaymentProcessed(companyId, sale.InvoiceNumber, sale.TotalAmount, "Cash");
            await _hub.NotifyDashboardRefresh(companyId, "Sales");
            await _hub.NotifyDashboardRefresh(companyId, "Finance");
            await _notificationService.NotifyCompanyActionAsync(
                user.Id, companyId, $"Payment received (Cash): {sale.InvoiceNumber} — ₱{sale.TotalAmount:N2}", "Finance", "bi-cash-coin");

            return Ok(new { success = true, message = "Cash payment processed. Inventory deducted." });
        }

        // ── Initiate Online Payment (PayMongo) ──────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Finance")]
        public async Task<IActionResult> InitiateOnlinePayment([FromBody] ProcessOnlinePaymentViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var companyId = await GetCompanyIdAsync();

            var sale = await _context.Sale
                .Include(s => s.Product)
                .FirstOrDefaultAsync(s => s.SaleID == model.SaleID && s.CompanyID == companyId);

            if (sale == null)
                return NotFound(new { success = false, message = "Sale not found." });

            if (sale.PaymentStatus == "Paid")
                return BadRequest(new { success = false, message = "This sale has already been paid." });

            var amountInCentavos = (int)(sale.TotalAmount * 100);
            var description = $"Payment for {sale.InvoiceNumber} — {sale.Product?.ProductName ?? "Product"}";
            var email = sale.BuyerEmail ?? "customer@dairyflow.com";

            var successUrl = Url.Action("OnlinePaymentSuccess", "Sales", new { saleId = sale.SaleID }, Request.Scheme)!;
            var cancelUrl = Url.Action("OnlinePaymentCancel", "Sales", new { saleId = sale.SaleID }, Request.Scheme)!;

            var result = await _payMongoService.CreateCheckoutSession(
                email, description, amountInCentavos, "gcash", successUrl, cancelUrl);

            if (result == null)
                return StatusCode(500, new { success = false, message = "Failed to create PayMongo checkout session." });

            // Store session ID on the sale for later verification 
            sale.PaymentMethod = "Online";

            // Create a pending transaction with the session ID
            var transaction = new SaleTransaction
            {
                SaleID = sale.SaleID,
                CompanyID = companyId,
                Amount = sale.TotalAmount,
                PaymentMethod = "Online",
                PaymentDate = DateTime.UtcNow,
                ProcessedByUserID = (await _userManager.GetUserAsync(User))?.Id ?? "",
                PayMongoSessionId = result.Value.sessionId,
                Notes = "Awaiting PayMongo payment"
            };

            _context.SaleTransaction.Add(transaction);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, checkoutUrl = result.Value.checkoutUrl });
        }

        // ── Online Payment Success Callback ──────────────────────────────

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> OnlinePaymentSuccess(int saleId)
        {
            var sale = await _context.Sale
                .Include(s => s.Inventory)
                .FirstOrDefaultAsync(s => s.SaleID == saleId);

            if (sale == null)
                return RedirectToAction("Index");

            // Find the pending transaction with PayMongo session
            var transaction = await _context.SaleTransaction
                .Where(t => t.SaleID == saleId && t.PayMongoSessionId != null)
                .OrderByDescending(t => t.TransactionID)
                .FirstOrDefaultAsync();

            if (transaction != null && !string.IsNullOrEmpty(transaction.PayMongoSessionId))
            {
                var verified = await _payMongoService.VerifyPayment(transaction.PayMongoSessionId);
                if (verified)
                {
                    sale.PaymentStatus = "Paid";
                    sale.PaymentMethod = "Online";
                    transaction.Notes = "Payment verified via PayMongo";
                    transaction.PaymentDate = DateTime.UtcNow;

                    // Deduct inventory
                    var inventory = await _context.Inventory.FindAsync(sale.InventoryID);
                    if (inventory != null)
                    {
                        inventory.Quantity = Math.Max(0, (inventory.Quantity ?? 0) - sale.Quantity);
                    }

                    await _context.SaveChangesAsync();

                    // Notifications
                    await _hub.NotifyPaymentProcessed(sale.CompanyID, sale.InvoiceNumber, sale.TotalAmount, "Online");
                    await _hub.NotifyDashboardRefresh(sale.CompanyID, "Sales");
                    await _hub.NotifyDashboardRefresh(sale.CompanyID, "Finance");
                    await _notificationService.NotifyCompanyActionAsync(
                        transaction.ProcessedByUserID, sale.CompanyID,
                        $"Online payment confirmed: {sale.InvoiceNumber} — ₱{sale.TotalAmount:N2}", "Finance", "bi-credit-card");
                }
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult OnlinePaymentCancel(int saleId)
        {
            TempData["ErrorMessage"] = "Online payment was cancelled.";
            return RedirectToAction("Index");
        }

        // ── Transaction History ──────────────────────────────────────────

        [HttpGet]
        [Authorize(Roles = "Admin,Finance")]
        public async Task<IActionResult> Transactions()
        {
            var companyId = await GetCompanyIdAsync();

            var transactions = await _context.SaleTransaction
                .Include(t => t.Sale)
                    .ThenInclude(s => s.Product)
                .Include(t => t.ProcessedByUser)
                .Where(t => t.CompanyID == companyId && t.Sale.PaymentStatus == "Paid")
                .OrderByDescending(t => t.PaymentDate)
                .Select(t => new TransactionListViewModel
                {
                    TransactionID = t.TransactionID,
                    SaleID = t.SaleID,
                    InvoiceNumber = t.Sale.InvoiceNumber,
                    ProductName = t.Sale.Product.ProductName ?? "Unknown",
                    BuyerName = t.Sale.BuyerName,
                    Amount = t.Amount,
                    PaymentMethod = t.PaymentMethod,
                    PaymentDate = t.PaymentDate,
                    ProcessedByName = t.ProcessedByUser.UserName ?? t.ProcessedByUser.Email ?? "Unknown",
                    ReferenceNumber = t.ReferenceNumber,
                    Notes = t.Notes
                })
                .ToListAsync();

            var summary = new TransactionSummaryViewModel
            {
                TotalTransactions = transactions.Count,
                TotalAmount = transactions.Sum(t => t.Amount),
                CashPayments = transactions.Count(t => t.PaymentMethod == "Cash"),
                OnlinePayments = transactions.Count(t => t.PaymentMethod == "Online")
            };

            var viewModel = new TransactionPageViewModel
            {
                Transactions = transactions,
                Summary = summary
            };

            return View(viewModel);
        }

        // ── Get Sale Detail (JSON) ───────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetSaleDetail(int id)
        {
            var companyId = await GetCompanyIdAsync();

            var sale = await _context.Sale
                .Include(s => s.Product)
                .Include(s => s.CreatedByUser)
                .FirstOrDefaultAsync(s => s.SaleID == id && s.CompanyID == companyId);

            if (sale == null)
                return NotFound();

            return Json(new SaleListViewModel
            {
                SaleID = sale.SaleID,
                InvoiceNumber = sale.InvoiceNumber,
                ProductName = sale.Product?.ProductName ?? "Unknown",
                BuyerName = sale.BuyerName,
                BuyerEmail = sale.BuyerEmail,
                BuyerPhone = sale.BuyerPhone,
                Quantity = sale.Quantity,
                UnitPrice = sale.UnitPrice,
                TotalAmount = sale.TotalAmount,
                SaleDate = sale.SaleDate,
                PaymentStatus = sale.PaymentStatus,
                PaymentMethod = sale.PaymentMethod,
                CreatedByName = sale.CreatedByUser?.UserName ?? "Unknown",
                Notes = sale.Notes
            });
        }

        // ── PDF Invoice Generation ───────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> DownloadInvoice(int id)
        {
            var companyId = await GetCompanyIdAsync();

            var sale = await _context.Sale
                .Include(s => s.Product)
                .Include(s => s.Company)
                .Include(s => s.CreatedByUser)
                .FirstOrDefaultAsync(s => s.SaleID == id && s.CompanyID == companyId);

            if (sale == null)
                return NotFound();

            var pdfBytes = GenerateInvoicePdf(sale);
            return File(pdfBytes, "application/pdf", $"{sale.InvoiceNumber}.pdf");
        }

        private byte[] GenerateInvoicePdf(Sale sale)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("INVOICE").Bold().FontSize(28).FontColor("#0d6efd");
                                c.Item().Text(sale.Company?.CompanyName ?? "DairyFlow").SemiBold().FontSize(14);
                            });
                            row.ConstantItem(150).AlignRight().Column(c =>
                            {
                                c.Item().Text(sale.InvoiceNumber).SemiBold().FontSize(12);
                                c.Item().Text($"Date: {sale.SaleDate:MMM dd, yyyy}").FontSize(10);
                                c.Item().PaddingTop(4).Text(text =>
                                {
                                    text.Span("Status: ").FontSize(10);
                                    text.Span(sale.PaymentStatus).Bold()
                                        .FontColor(sale.PaymentStatus == "Paid" ? "#198754" : "#dc3545")
                                        .FontSize(10);
                                });
                            });
                        });
                        col.Item().PaddingTop(10).LineHorizontal(1).LineColor("#dee2e6");
                    });

                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        // Buyer info
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Bill To:").SemiBold().FontSize(11);
                                c.Item().Text(sale.BuyerName).FontSize(11);
                                if (!string.IsNullOrEmpty(sale.BuyerEmail))
                                    c.Item().Text(sale.BuyerEmail).FontSize(10).FontColor("#6c757d");
                                if (!string.IsNullOrEmpty(sale.BuyerPhone))
                                    c.Item().Text(sale.BuyerPhone).FontSize(10).FontColor("#6c757d");
                            });
                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text("Created By:").SemiBold().FontSize(11);
                                c.Item().Text(sale.CreatedByUser?.UserName ?? "Unknown").FontSize(10);
                            });
                        });

                        col.Item().PaddingTop(20);

                        // Item table
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(4);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                            });

                            // Header
                            table.Header(header =>
                            {
                                header.Cell().Background("#0d6efd").Padding(6)
                                    .Text("Product").FontColor("#ffffff").SemiBold().FontSize(10);
                                header.Cell().Background("#0d6efd").Padding(6)
                                    .Text("Qty").FontColor("#ffffff").SemiBold().FontSize(10).AlignCenter();
                                header.Cell().Background("#0d6efd").Padding(6)
                                    .Text("Unit Price").FontColor("#ffffff").SemiBold().FontSize(10).AlignRight();
                                header.Cell().Background("#0d6efd").Padding(6)
                                    .Text("Total").FontColor("#ffffff").SemiBold().FontSize(10).AlignRight();
                            });

                            // Row
                            table.Cell().BorderBottom(1).BorderColor("#dee2e6").Padding(6)
                                .Text(sale.Product?.ProductName ?? "Product").FontSize(10);
                            table.Cell().BorderBottom(1).BorderColor("#dee2e6").Padding(6)
                                .Text(sale.Quantity.ToString()).FontSize(10).AlignCenter();
                            table.Cell().BorderBottom(1).BorderColor("#dee2e6").Padding(6)
                                .Text($"₱{sale.UnitPrice:N2}").FontSize(10).AlignRight();
                            table.Cell().BorderBottom(1).BorderColor("#dee2e6").Padding(6)
                                .Text($"₱{sale.TotalAmount:N2}").FontSize(10).AlignRight();
                        });

                        col.Item().PaddingTop(10).AlignRight().Column(c =>
                        {
                            c.Item().Row(r =>
                            {
                                r.RelativeItem();
                                r.ConstantItem(200).Row(inner =>
                                {
                                    inner.RelativeItem().Text("Subtotal:").SemiBold().FontSize(11);
                                    inner.RelativeItem().AlignRight().Text($"₱{sale.TotalAmount:N2}").FontSize(11);
                                });
                            });
                            c.Item().PaddingTop(4).Row(r =>
                            {
                                r.RelativeItem();
                                r.ConstantItem(200).LineHorizontal(2).LineColor("#0d6efd");
                            });
                            c.Item().PaddingTop(4).Row(r =>
                            {
                                r.RelativeItem();
                                r.ConstantItem(200).Row(inner =>
                                {
                                    inner.RelativeItem().Text("Total Due:").Bold().FontSize(14);
                                    inner.RelativeItem().AlignRight().Text($"₱{sale.TotalAmount:N2}").Bold().FontSize(14).FontColor("#0d6efd");
                                });
                            });
                        });

                        if (!string.IsNullOrEmpty(sale.Notes))
                        {
                            col.Item().PaddingTop(30).Column(c =>
                            {
                                c.Item().Text("Notes:").SemiBold().FontSize(10);
                                c.Item().Text(sale.Notes).FontSize(10).FontColor("#6c757d");
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Generated by DairyFlow ERP • ").FontSize(8).FontColor("#6c757d");
                        text.Span(DateTime.Now.ToString("MMM dd, yyyy HH:mm")).FontSize(8).FontColor("#6c757d");
                    });
                });
            }).GeneratePdf();
        }

        // ── Expired Products ─────────────────────────────────────────────

        [HttpGet]
        [Authorize(Roles = "Admin,ProductManager,QualityChecker")]
        public async Task<IActionResult> ExpiredProducts()
        {
            var companyId = await GetCompanyIdAsync();
            var today = DateTime.UtcNow.Date;

            var expired = await _context.Inventory
                .Include(i => i.Product)
                .Include(i => i.User)
                .Where(i => i.CompanyID == companyId && i.Expiry.HasValue && i.Expiry.Value.Date <= today && (i.Quantity ?? 0) > 0)
                .OrderBy(i => i.Expiry)
                .Select(i => new ExpiredProductViewModel
                {
                    InventoryID = i.InventoryID,
                    ProductID = i.ProductID,
                    ProductName = i.Product.ProductName ?? "Unknown",
                    ProductType = i.Product.Type ?? "Other",
                    Quantity = i.Quantity ?? 0,
                    Expiry = i.Expiry,
                    DaysExpired = (int)(today - i.Expiry!.Value.Date).TotalDays,
                    UserName = i.User.UserName ?? i.User.Email ?? "Unknown"
                })
                .ToListAsync();

            var viewModel = new ExpiredProductsPageViewModel
            {
                Items = expired,
                TotalExpiredItems = expired.Count,
                TotalExpiredQuantity = expired.Sum(e => e.Quantity)
            };

            return View(viewModel);
        }

        // ── Deduct Expired Inventory ─────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProductManager")]
        public async Task<IActionResult> DeductExpired([FromBody] DeductExpiredViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var companyId = await GetCompanyIdAsync();

            var inventory = await _context.Inventory
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.InventoryID == model.InventoryID && i.CompanyID == companyId);

            if (inventory == null)
                return NotFound(new { success = false, message = "Inventory item not found." });

            if ((inventory.Quantity ?? 0) < model.Quantity)
                return BadRequest(new { success = false, message = "Deduction quantity exceeds available stock." });

            inventory.Quantity = (inventory.Quantity ?? 0) - model.Quantity;
            await _context.SaveChangesAsync();

            // Notification
            await _hub.NotifyInventoryUpdated(companyId, "Expired deduction", inventory.Product?.ProductName ?? "Unknown", user.UserName ?? "");
            await _hub.NotifyDashboardRefresh(companyId, "Inventory");
            await _notificationService.NotifyCompanyActionAsync(
                user.Id, companyId,
                $"Expired stock deducted: {model.Quantity} units of {inventory.Product?.ProductName ?? "Unknown"}",
                "Inventory", "bi-exclamation-triangle");

            return Ok(new { success = true, message = $"Deducted {model.Quantity} units of expired stock." });
        }
    }
}
