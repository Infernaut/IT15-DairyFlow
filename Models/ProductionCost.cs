using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_DairyFlow.Models
{
    public class ProductionCost
    {
        [Key]
        public int ProductionCostID { get; set; }

        [Required]
        public int ProductionBatchID { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? TotalCost { get; set; }

        [Required]
        public int CompanyID { get; set; }

        // Navigation properties
        [ForeignKey("ProductionBatchID")]
        public virtual ProductionBatch ProductionBatch { get; set; } = null!;

        [ForeignKey("CompanyID")]
        public virtual Company Company { get; set; } = null!;
    }
}
