using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_DairyFlow.Models
{
    public class RawMaterial
    {
        [Key]
        public int RawMaterialID { get; set; }

        [Required]
        public int SupplierID { get; set; }

        [Required]
        public int CompanyID { get; set; }

        [MaxLength(256)]
        public string? MaterialName { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? UnitCost { get; set; }

        [MaxLength(50)]
        public string? Unit { get; set; } = "kg";

        public int? CurrentStock { get; set; } = 0;

        public int? MinimumStock { get; set; } = 10;

        public DateTime? LastRestockDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("SupplierID")]
        public virtual Supplier Supplier { get; set; } = null!;

        [ForeignKey("CompanyID")]
        public virtual Company Company { get; set; } = null!;
    }
}
