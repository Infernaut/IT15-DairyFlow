using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_DairyFlow.Models
{
    public class ProductionBatch
    {
        [Key]
        public int ProductionBatchID { get; set; }

        [Required]
        public int CompanyID { get; set; }

        [MaxLength(100)]
        public string? BatchCode { get; set; }

        public DateTime? Date { get; set; }

        [MaxLength(50)]
        public string? Status { get; set; }

        // Navigation properties
        [ForeignKey("CompanyID")]
        public virtual Company Company { get; set; } = null!;

        public virtual ICollection<ProductionCost> ProductionCosts { get; set; } = new List<ProductionCost>();
        public virtual ICollection<QualityInspection> QualityInspections { get; set; } = new List<QualityInspection>();
    }
}
