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
}
