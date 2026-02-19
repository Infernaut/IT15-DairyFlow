using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_DairyFlow.Models
{
    public class QualityInspection
    {
        [Key]
        public int QualityInspectionID { get; set; }

        [Required]
        public int CompanyID { get; set; }

        [Required]
        public int ProductionBatchID { get; set; }

        [MaxLength(100)]
        public string? Type { get; set; }

        [MaxLength(100)]
        public string? Result { get; set; }

        // Navigation properties
        [ForeignKey("CompanyID")]
        public virtual Company Company { get; set; } = null!;

        [ForeignKey("ProductionBatchID")]
        public virtual ProductionBatch ProductionBatch { get; set; } = null!;
    }
}
