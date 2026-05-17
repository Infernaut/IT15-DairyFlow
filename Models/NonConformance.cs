using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_DairyFlow.Models
{
    public class NonConformance
    {
        [Key]
        public int NonConformanceID { get; set; }

        [Required]
        public int CompanyID { get; set; }

        public int? QualityInspectionID { get; set; }

        public int? ProductionBatchID { get; set; }

        [Required]
        [MaxLength(450)]
        public string ReportedByUserId { get; set; } = string.Empty;

        [MaxLength(450)]
        public string? AssignedToUserId { get; set; }

        // NCR Number (auto-generated)
        [Required]
        [MaxLength(50)]
        public string NCRNumber { get; set; } = string.Empty;

        // Category: Material, Process, Equipment, Product, Packaging, Labeling
        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        // Severity: Critical, Major, Minor
        [Required]
        [MaxLength(20)]
        public string Severity { get; set; } = "Minor";

        // Title/Summary
        [Required]
        [MaxLength(200)]
    [NotMapped]
    public string Title { get; set; } = string.Empty;

    public string? TitleEncrypted { get; set; }

        // Detailed description
        [MaxLength(2000)]
    [NotMapped]
    public string? Description { get; set; }

    public string? DescriptionEncrypted { get; set; }

        // Root Cause Analysis
        [MaxLength(2000)]
    [NotMapped]
    public string? RootCause { get; set; }

    public string? RootCauseEncrypted { get; set; }

        // Immediate corrective action taken
        [MaxLength(2000)]
    [NotMapped]
    public string? ImmediateAction { get; set; }

    public string? ImmediateActionEncrypted { get; set; }

        // Long-term corrective action / CAPA
        [MaxLength(2000)]
    [NotMapped]
    public string? CorrectiveAction { get; set; }

    public string? CorrectiveActionEncrypted { get; set; }

        // Preventive action
        [MaxLength(2000)]
    [NotMapped]
    public string? PreventiveAction { get; set; }

    public string? PreventiveActionEncrypted { get; set; }

        // Status: Open, InProgress, PendingVerification, Closed
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Open";

        // Affected quantity
        public int? AffectedQuantity { get; set; }

        // Cost impact
        public decimal? CostImpact { get; set; }

        // Disposition: UseAsIs, Rework, Scrap, Return, Hold
        [MaxLength(50)]
        public string? Disposition { get; set; }

        // Dates
        public DateTime ReportedDate { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }
        public DateTime? ClosedDate { get; set; }
        public DateTime? VerifiedDate { get; set; }

        [MaxLength(450)]
        public string? VerifiedByUserId { get; set; }

        // Navigation properties
        [ForeignKey("CompanyID")]
        public virtual Company Company { get; set; } = null!;

        [ForeignKey("QualityInspectionID")]
        public virtual QualityInspection? QualityInspection { get; set; }

        [ForeignKey("ProductionBatchID")]
        public virtual ProductionBatch? ProductionBatch { get; set; }

        [ForeignKey("ReportedByUserId")]
        public virtual ApplicationUser ReportedByUser { get; set; } = null!;

        [ForeignKey("AssignedToUserId")]
        public virtual ApplicationUser? AssignedToUser { get; set; }

        [ForeignKey("VerifiedByUserId")]
        public virtual ApplicationUser? VerifiedByUser { get; set; }
    }
}
