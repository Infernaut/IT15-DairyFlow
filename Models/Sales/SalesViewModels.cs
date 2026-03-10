using System.ComponentModel.DataAnnotations;

namespace IT15_DairyFlow.Models.Sales
{
    // ── Page View Models ──────────────────────────────────────────────

    public class SalesPageViewModel
    {
        public List<SaleListViewModel> Sales { get; set; } = new List<SaleListViewModel>();
        public List<InventoryProductLookupViewModel> AvailableProducts { get; set; } = new List<InventoryProductLookupViewModel>();
        public SalesSummaryViewModel Summary { get; set; } = new SalesSummaryViewModel();
    }

    public class SalesSummaryViewModel
    {
        public int TotalSales { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal PendingPayments { get; set; }
        public int PaidCount { get; set; }
        public int PendingCount { get; set; }
    }

    public class SaleListViewModel
    {
        public int SaleID { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string BuyerName { get; set; } = string.Empty;
        public string? BuyerEmail { get; set; }
        public string? BuyerPhone { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime SaleDate { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class InventoryProductLookupViewModel
    {
        public int InventoryID { get; set; }
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int AvailableQuantity { get; set; }
    }

    // ── Create / Edit View Models ─────────────────────────────────────

    public class CreateSaleViewModel
    {
        [Required]
        public int InventoryID { get; set; }

        [Required]
        [MaxLength(256)]
        public string BuyerName { get; set; } = string.Empty;

        [MaxLength(256)]
        public string? BuyerEmail { get; set; }

        [MaxLength(50)]
        public string? BuyerPhone { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
        public decimal UnitPrice { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    // ── Payment Processing ────────────────────────────────────────────

    public class ProcessCashPaymentViewModel
    {
        [Required]
        public int SaleID { get; set; }

        [MaxLength(100)]
        public string? ReferenceNumber { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    public class ProcessOnlinePaymentViewModel
    {
        [Required]
        public int SaleID { get; set; }
    }

    // ── Transaction History ───────────────────────────────────────────

    public class TransactionPageViewModel
    {
        public List<TransactionListViewModel> Transactions { get; set; } = new List<TransactionListViewModel>();
        public TransactionSummaryViewModel Summary { get; set; } = new TransactionSummaryViewModel();
    }

    public class TransactionListViewModel
    {
        public int TransactionID { get; set; }
        public int SaleID { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string BuyerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string ProcessedByName { get; set; } = string.Empty;
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }
    }

    public class TransactionSummaryViewModel
    {
        public int TotalTransactions { get; set; }
        public decimal TotalAmount { get; set; }
        public int CashPayments { get; set; }
        public int OnlinePayments { get; set; }
    }

    // ── Expired Products ──────────────────────────────────────────────

    public class ExpiredProductsPageViewModel
    {
        public List<ExpiredProductViewModel> Items { get; set; } = new List<ExpiredProductViewModel>();
        public int TotalExpiredItems { get; set; }
        public int TotalExpiredQuantity { get; set; }
    }

    public class ExpiredProductViewModel
    {
        public int InventoryID { get; set; }
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime? Expiry { get; set; }
        public int DaysExpired { get; set; }
        public string UserName { get; set; } = string.Empty;
    }

    public class DeductExpiredViewModel
    {
        [Required]
        public int InventoryID { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
