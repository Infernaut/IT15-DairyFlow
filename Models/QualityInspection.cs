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

        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Type { get; set; }

        [MaxLength(100)]
        public string? Result { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "ongoing";

        // Navigation properties
        [ForeignKey("CompanyID")]
        public virtual Company Company { get; set; } = null!;

        [ForeignKey("ProductionBatchID")]
        public virtual ProductionBatch ProductionBatch { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
