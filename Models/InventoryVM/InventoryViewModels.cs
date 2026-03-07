using System.ComponentModel.DataAnnotations;

namespace IT15_DairyFlow.Models.InventoryVM
{
    public class InventoryPageViewModel
    {
        public List<InventoryListViewModel> Items { get; set; } = new List<InventoryListViewModel>();
        public List<ProductLookupViewModel> Products { get; set; } = new List<ProductLookupViewModel>();
        public InventorySummaryViewModel Summary { get; set; } = new InventorySummaryViewModel();
    }

    public class InventoryListViewModel
    {
        public int InventoryID { get; set; }
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime? Expiry { get; set; }
        public string StockStatus { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool IsExpiringSoon { get; set; }
        public bool IsExpired { get; set; }
        public int DaysUntilExpiry { get; set; }
    }

    public class InventoryDetailViewModel
    {
        public int InventoryID { get; set; }
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime? Expiry { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserID { get; set; } = string.Empty;
        public DateTime? LastUpdated { get; set; }
    }

    public class CreateInventoryViewModel
    {
        [Required]
        public int ProductID { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be a positive number")]
        public int Quantity { get; set; }

        public DateTime? Expiry { get; set; }
    }

    public class UpdateInventoryViewModel
    {
        [Required]
        public int InventoryID { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be a positive number")]
        public int Quantity { get; set; }

        public DateTime? Expiry { get; set; }
    }

    public class AdjustInventoryViewModel
    {
        [Required]
        public int InventoryID { get; set; }

        [Required]
        public int AdjustmentQuantity { get; set; }

        [Required]
        [MaxLength(50)]
        public string AdjustmentType { get; set; } = string.Empty; // "Add", "Remove", "Damaged", "Expired"

        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    public class InventorySummaryViewModel
    {
        public int TotalItems { get; set; }
        public int TotalQuantity { get; set; }
        public int LowStockItems { get; set; }
        public int ExpiringSoonItems { get; set; }
        public int ExpiredItems { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class ProductLookupViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class InventoryTransactionViewModel
    {
        public int TransactionID { get; set; }
        public int InventoryID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime TransactionDate { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
