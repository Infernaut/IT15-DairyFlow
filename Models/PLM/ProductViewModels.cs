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
        public string UserEmail { get; set; } = string.Empty;
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
        public string UserEmail { get; set; } = string.Empty;
        
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
    }

    public class BatchMaterialRequirementViewModel
    {
        public string MaterialName { get; set; } = string.Empty;
        public decimal RequiredQuantity { get; set; }
        public string Unit { get; set; } = "kg";
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
    }
}
