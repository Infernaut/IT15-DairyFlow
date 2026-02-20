using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_DairyFlow.Models
{
    [Table("ProductionBatch")]
    public class ProductionBatch
    {
        [Key]
        public int ProductionBatchID { get; set; }

        [Required]
        public int CompanyID { get; set; }

        [Required]
        public int ProductID { get; set; }

        [Required]
        public int EquipmentID { get; set; }

        [Required]
        [MaxLength(450)]
        public string UserID { get; set; } = string.Empty;

        [MaxLength(100)]
        [Column("batchCode")]
        public string? BatchCode { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [MaxLength(50)]
        [Column("status")]
        public string? Status { get; set; }

        // Navigation properties
        [ForeignKey("CompanyID")]
        public virtual Company Company { get; set; } = null!;

        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; } = null!;

        [ForeignKey("EquipmentID")]
        public virtual Equipment Equipment { get; set; } = null!;

        [ForeignKey("UserID")]
        public virtual ApplicationUser User { get; set; } = null!;

        public virtual ICollection<ProductionCost> ProductionCosts { get; set; } = new List<ProductionCost>();
        public virtual ICollection<QualityInspection> QualityInspections { get; set; } = new List<QualityInspection>();
    }
}
