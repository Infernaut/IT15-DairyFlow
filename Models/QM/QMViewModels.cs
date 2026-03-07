using System.ComponentModel.DataAnnotations;

namespace IT15_DairyFlow.Models.QM
{
    public class QualityInspectionViewModel
    {
        public int QualityInspectionID { get; set; }
        public int ProductionBatchID { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string InspectionType { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime? InspectionDate { get; set; }
    }

    public class QualityInspectionPageViewModel
    {
        public List<QualityInspectionViewModel> Inspections { get; set; } = new List<QualityInspectionViewModel>();
        public QualityInspectionSummaryViewModel Summary { get; set; } = new QualityInspectionSummaryViewModel();
        public List<UserLookupViewModel> QualityCheckers { get; set; } = new List<UserLookupViewModel>();
    }

    public class QualityInspectionSummaryViewModel
    {
        public int TotalPending { get; set; }
        public int TotalOnHold { get; set; }
        public int PassedToday { get; set; }
        public int FailedToday { get; set; }
        public int OpenNCRs { get; set; }
    }

    public class QualityInspectionResultViewModel
    {
        public int QualityInspectionID { get; set; }
        public int ProductionBatchID { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string InspectionType { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime? InspectionDate { get; set; }
        public DateTime? CompletedDate { get; set; }
    }

    public class QualityInspectionResultsPageViewModel
    {
        public List<QualityInspectionResultViewModel> Results { get; set; } = new List<QualityInspectionResultViewModel>();
    }

    public class QualityInspectionDetailViewModel
    {
        public int QualityInspectionID { get; set; }
        public int ProductionBatchID { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string InspectionType { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? SampleLot { get; set; }
        public int? SampleSize { get; set; }
        public DateTime? InspectionDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string? Notes { get; set; }

        // Test parameters
        public decimal? Temperature { get; set; }
        public decimal? PHLevel { get; set; }
        public decimal? FatContent { get; set; }
        public decimal? ProteinContent { get; set; }
        public decimal? MoistureContent { get; set; }
        public decimal? Acidity { get; set; }
        public int? BacterialCount { get; set; }
        public int? SomaticCellCount { get; set; }
        public bool? AntibioticTest { get; set; }
    }

    public class UserLookupViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateInspectionUserViewModel
    {
        [Required]
        public int InspectionID { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
    }

    public class PerformInspectionViewModel
    {
        [Required]
        public int InspectionID { get; set; }
        
        public List<string> TestTypes { get; set; } = new List<string>();
        
        [Required]
        public string Result { get; set; } = string.Empty;

        public string? SampleLot { get; set; }
        public int? SampleSize { get; set; }
        public string? Notes { get; set; }

        // Test parameters
        public decimal? Temperature { get; set; }
        public decimal? PHLevel { get; set; }
        public decimal? FatContent { get; set; }
        public decimal? ProteinContent { get; set; }
        public decimal? MoistureContent { get; set; }
        public decimal? Acidity { get; set; }
        public int? BacterialCount { get; set; }
        public int? SomaticCellCount { get; set; }
        public bool? AntibioticTest { get; set; }
    }

    // Hold/Release ViewModels
    public class HoldBatchViewModel
    {
        [Required]
        public int InspectionID { get; set; }

        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
    }

    public class ReleaseBatchViewModel
    {
        [Required]
        public int InspectionID { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    // NonConformance ViewModels
    public class NonConformanceViewModel
    {
        public int NonConformanceID { get; set; }
        public string NCRNumber { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ReportedBy { get; set; } = string.Empty;
        public string? AssignedTo { get; set; }
        public DateTime ReportedDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string? BatchCode { get; set; }
    }

    public class NonConformancePageViewModel
    {
        public List<NonConformanceViewModel> NonConformances { get; set; } = new List<NonConformanceViewModel>();
        public NonConformanceSummaryViewModel Summary { get; set; } = new NonConformanceSummaryViewModel();
        public List<UserLookupViewModel> Users { get; set; } = new List<UserLookupViewModel>();
    }

    public class NonConformanceSummaryViewModel
    {
        public int TotalOpen { get; set; }
        public int TotalInProgress { get; set; }
        public int PendingVerification { get; set; }
        public int ClosedThisMonth { get; set; }
        public int OverdueCount { get; set; }
        public decimal TotalCostImpact { get; set; }
    }

    public class NonConformanceDetailViewModel
    {
        public int NonConformanceID { get; set; }
        public string NCRNumber { get; set; } = string.Empty;
        public int? QualityInspectionID { get; set; }
        public int? ProductionBatchID { get; set; }
        public string? BatchCode { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? RootCause { get; set; }
        public string? ImmediateAction { get; set; }
        public string? CorrectiveAction { get; set; }
        public string? PreventiveAction { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? AffectedQuantity { get; set; }
        public decimal? CostImpact { get; set; }
        public string? Disposition { get; set; }
        public DateTime ReportedDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? ClosedDate { get; set; }
        public string ReportedById { get; set; } = string.Empty;
        public string ReportedByName { get; set; } = string.Empty;
        public string? AssignedToId { get; set; }
        public string? AssignedToName { get; set; }
    }

    public class CreateNonConformanceViewModel
    {
        public int? QualityInspectionID { get; set; }
        public int? ProductionBatchID { get; set; }

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Severity { get; set; } = "Minor";

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [MaxLength(2000)]
        public string? ImmediateAction { get; set; }

        public int? AffectedQuantity { get; set; }
        public string? AssignedToUserId { get; set; }
        public DateTime? DueDate { get; set; }
    }

    public class UpdateNonConformanceViewModel
    {
        [Required]
        public int NonConformanceID { get; set; }

        [MaxLength(2000)]
        public string? RootCause { get; set; }

        [MaxLength(2000)]
        public string? CorrectiveAction { get; set; }

        [MaxLength(2000)]
        public string? PreventiveAction { get; set; }

        [MaxLength(50)]
        public string? Disposition { get; set; }

        public decimal? CostImpact { get; set; }

        public string? AssignedToUserId { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Status { get; set; }
    }
}
