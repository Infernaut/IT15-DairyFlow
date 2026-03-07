using System.ComponentModel.DataAnnotations;

namespace IT15_DairyFlow.Models.Production
{
    public class ProductionBatchPageViewModel
    {
        public IReadOnlyList<ProductionBatchListViewModel> Batches { get; set; } = new List<ProductionBatchListViewModel>();
        public IReadOnlyList<LookupItemViewModel> Products { get; set; } = new List<LookupItemViewModel>();
        public IReadOnlyList<LookupItemViewModel> Equipments { get; set; } = new List<LookupItemViewModel>();
        public bool ShowArchived { get; set; }
    }

    public class ProductionBatchListViewModel
    {
        public int ProductionBatchID { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string EquipmentName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal? TotalCost { get; set; }
    }

    public class ProductionBatchDetailViewModel
    {
        public int ProductionBatchID { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string EquipmentName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal? TotalCost { get; set; }
    }

    public class CreateProductionBatchViewModel
    {
        [Required]
        public DateTime? StartDate { get; set; }

        [Required]
        public DateTime? EndDate { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "In Production";

        [Required]
        public int ProductID { get; set; }

        [Required]
        public int EquipmentID { get; set; }

        [Required]
        public int Quantity { get; set; } = 0;
    }

    public class ProductionScheduleItemViewModel
    {
        public int ProductionBatchID { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string EquipmentName { get; set; } = string.Empty;
    }

    public class ProductionSchedulePageViewModel
    {
        public IReadOnlyList<ProductionScheduleItemViewModel> ScheduleItems { get; set; } = new List<ProductionScheduleItemViewModel>();
        public IReadOnlyList<LookupItemViewModel> Products { get; set; } = new List<LookupItemViewModel>();
        public IReadOnlyList<LookupItemViewModel> Equipments { get; set; } = new List<LookupItemViewModel>();
    }

    public class ProductionCostPageViewModel
    {
        public IReadOnlyList<ProductionCostListViewModel> CostItems { get; set; } = new List<ProductionCostListViewModel>();
        public decimal TotalCostAll { get; set; }
        public decimal TotalCostToday { get; set; }
        public decimal TotalCostWeek { get; set; }
        public decimal TotalCostMonth { get; set; }
        public decimal TotalCostYear { get; set; }
        public int TotalBatches { get; set; }
        public decimal AverageCostPerBatch { get; set; }
    }

    public class ProductionCostListViewModel
    {
        public int ProductionCostID { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal? TotalCost { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class ProductionHistoryItemViewModel
    {
        public int ProductionBatchID { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string EquipmentName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal? TotalCost { get; set; }
    }

    public class LookupItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool HasFormulation { get; set; }
    }

    // Supplier ViewModels
    public class SupplierPageViewModel
    {
        public IReadOnlyList<SupplierListViewModel> Suppliers { get; set; } = new List<SupplierListViewModel>();
    }

    public class SupplierListViewModel
    {
        public int SupplierID { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string? ContactInfo { get; set; }
        public string? Status { get; set; }
    }

    public class CreateSupplierViewModel
    {
        [Required]
        [StringLength(256)]
        public string SupplierName { get; set; } = string.Empty;

        public string? ContactInfo { get; set; }
    }

    public class UpdateSupplierViewModel
    {
        [Required]
        public int SupplierID { get; set; }

        [Required]
        [StringLength(256)]
        public string SupplierName { get; set; } = string.Empty;

        public string? ContactInfo { get; set; }
    }
}
