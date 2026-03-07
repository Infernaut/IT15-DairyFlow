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

        // Inspection Type: Incoming, InProcess, FinalProduct, Environmental
        [MaxLength(50)]
        public string? InspectionType { get; set; }

        // Test types performed (comma-separated or stored tests)
        [MaxLength(500)]
        public string? Type { get; set; }

        // Pass, Fail, Pending
        [MaxLength(100)]
        public string? Result { get; set; }

        // ongoing, completed, on-hold, released, rejected
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "ongoing";

        // Sample information
        [MaxLength(100)]
        public string? SampleLot { get; set; }

        public int? SampleSize { get; set; }

        // Test Parameters for Dairy
        public decimal? Temperature { get; set; }        // °C
        public decimal? PHLevel { get; set; }            // pH scale
        public decimal? FatContent { get; set; }         // percentage
        public decimal? ProteinContent { get; set; }     // percentage
        public decimal? MoistureContent { get; set; }    // percentage
        public decimal? Acidity { get; set; }            // percentage
        public int? BacterialCount { get; set; }         // CFU/ml
        public int? SomaticCellCount { get; set; }       // cells/ml
        public bool? AntibioticTest { get; set; }        // true = positive (fail), false = negative (pass)

        // Acceptable ranges (stored as JSON or simple values)
        [MaxLength(500)]
        public string? AcceptableRanges { get; set; }

        // Inspection dates
        public DateTime? InspectionDate { get; set; }
        public DateTime? CompletedDate { get; set; }

        // Notes and remarks
        [MaxLength(2000)]
        public string? Notes { get; set; }

        [MaxLength(500)]
        public string? CorrectiveAction { get; set; }

        // Navigation properties
        [ForeignKey("CompanyID")]
        public virtual Company Company { get; set; } = null!;

        [ForeignKey("ProductionBatchID")]
        public virtual ProductionBatch ProductionBatch { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        public virtual ICollection<NonConformance> NonConformances { get; set; } = new List<NonConformance>();
    }
}
