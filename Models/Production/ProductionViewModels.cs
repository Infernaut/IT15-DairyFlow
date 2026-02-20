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

    public class ProductionCostListViewModel
    {
        public int ProductionCostID { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public decimal? TotalCost { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class LookupItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
