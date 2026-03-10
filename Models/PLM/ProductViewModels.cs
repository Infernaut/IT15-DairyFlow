using System.ComponentModel.DataAnnotations;

namespace IT15_DairyFlow.Models.PLM
{
    public class ProductListViewModel
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? LifecycleStatus { get; set; }
        public int CompanyID { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int? ShelfLifeDays { get; set; }
    }

    public class ProductDetailViewModel
    {
        public int ProductID { get; set; }
        
        [Required]
        [Display(Name = "Product Name")]
        [StringLength(256)]
        public string ProductName { get; set; } = string.Empty;
        
        [Display(Name = "Type")]
        [StringLength(100)]
        public string? Type { get; set; }
        
        [Display(Name = "Lifecycle Status")]
        [StringLength(50)]
        public string? LifecycleStatus { get; set; }
        
        public int CompanyID { get; set; }
        public string UserID { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int? ShelfLifeDays { get; set; }
        
        // Formulation details
        public List<FormulationIngredientViewModel> Ingredients { get; set; } = new List<FormulationIngredientViewModel>();
    }

    public class CreateProductViewModel
    {
        [Required]
        [Display(Name = "Product Name")]
        [StringLength(256)]
        public string ProductName { get; set; } = string.Empty;
        
        [Display(Name = "Type")]
        [StringLength(100)]
        public string? Type { get; set; }

        [Display(Name = "Shelf Life (Days)")]
        [Range(1, 3650, ErrorMessage = "Shelf life must be between 1 and 3650 days.")]
        public int? ShelfLifeDays { get; set; }
    }

    public class EditProductViewModel
    {
        public int ProductID { get; set; }
        
        [Required]
        [Display(Name = "Product Name")]
        [StringLength(256)]
        public string ProductName { get; set; } = string.Empty;
        
        [Display(Name = "Type")]
        [StringLength(100)]
        public string? Type { get; set; }
        
        [Display(Name = "Lifecycle Status")]
        [StringLength(50)]
        public string? LifecycleStatus { get; set; }

        [Display(Name = "Shelf Life (Days)")]
        [Range(1, 3650, ErrorMessage = "Shelf life must be between 1 and 3650 days.")]
        public int? ShelfLifeDays { get; set; }
    }

    // Formulation View Models
    public class FormulationPageViewModel
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductType { get; set; }
        public string? LifecycleStatus { get; set; }
        public List<FormulationIngredientViewModel> Ingredients { get; set; } = new List<FormulationIngredientViewModel>();
        public List<RawMaterialLookupViewModel> AvailableMaterials { get; set; } = new List<RawMaterialLookupViewModel>();
        public decimal TotalCost { get; set; }
    }

    public class FormulationIngredientViewModel
    {
        public int FormulationID { get; set; }
        public int RawMaterialID { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "kg";
        public int? ProcessOrder { get; set; }
        public string? ProcessInstructions { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost => Quantity * UnitCost;
    }

    public class CreateFormulationViewModel
    {
        [Required]
        public int ProductID { get; set; }

        [Required]
        public int RawMaterialID { get; set; }

        [Required]
        [Range(0.0001, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal Quantity { get; set; }

        [MaxLength(50)]
        public string Unit { get; set; } = "kg";

        public int? ProcessOrder { get; set; }

        [MaxLength(500)]
        public string? ProcessInstructions { get; set; }
    }

    public class UpdateFormulationViewModel
    {
        [Required]
        public int FormulationID { get; set; }

        [Required]
        [Range(0.0001, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal Quantity { get; set; }

        [MaxLength(50)]
        public string Unit { get; set; } = "kg";

        public int? ProcessOrder { get; set; }

        [MaxLength(500)]
        public string? ProcessInstructions { get; set; }
    }

    public class RawMaterialLookupViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal UnitCost { get; set; }
        public string Unit { get; set; } = "kg";
    }

    public class BatchMaterialRequirementViewModel
    {
        public string MaterialName { get; set; } = string.Empty;
        public decimal RequiredQuantity { get; set; }
        public string Unit { get; set; } = "kg";
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
    }

    // ─── PRODUCT APPROVAL VIEW MODELS ─────────────────────────
    public class ProductApprovalListViewModel
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? LifecycleStatus { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public string CreatedByEmail { get; set; } = string.Empty;
        public DateTime? CreatedDate { get; set; }
    }

    public class ProductApprovalDetailViewModel
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? LifecycleStatus { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public string CreatedByEmail { get; set; } = string.Empty;
        public List<FormulationIngredientViewModel> Ingredients { get; set; } = new();
        public decimal TotalUnitCost { get; set; }
    }

    public class ProductApprovalActionViewModel
    {
        [Required]
        public int ProductID { get; set; }

        [Required]
        public string Action { get; set; } = string.Empty; // "Approved" or "Rejected"

        [MaxLength(500)]
        public string? Reason { get; set; }
    }
}
