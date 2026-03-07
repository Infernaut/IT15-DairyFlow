using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_DairyFlow.Models
{
    /// <summary>
    /// Represents a product formulation/recipe with ingredients and their quantities
    /// </summary>
    [Table("ProductFormulation")]
    public class ProductFormulation
    {
        [Key]
        public int FormulationID { get; set; }

        [Required]
        public int ProductID { get; set; }

        [Required]
        public int CompanyID { get; set; }

        [Required]
        public int RawMaterialID { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 4)")]
        public decimal Quantity { get; set; }

        [MaxLength(50)]
        public string? Unit { get; set; } = "kg";

        public int? ProcessOrder { get; set; }

        [MaxLength(500)]
        public string? ProcessInstructions { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; } = null!;

        [ForeignKey("CompanyID")]
        public virtual Company Company { get; set; } = null!;

        [ForeignKey("RawMaterialID")]
        public virtual RawMaterial RawMaterial { get; set; } = null!;
    }
}
