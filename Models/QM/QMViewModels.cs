using System.ComponentModel.DataAnnotations;

namespace IT15_DairyFlow.Models.QM
{
    public class QualityInspectionViewModel
    {
        public int QualityInspectionID { get; set; }
        public int ProductionBatchID { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class QualityInspectionPageViewModel
    {
        public List<QualityInspectionViewModel> Inspections { get; set; } = new List<QualityInspectionViewModel>();
    }

    public class QualityInspectionResultViewModel
    {
        public int QualityInspectionID { get; set; }
        public int ProductionBatchID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
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
        public string Result { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
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
        
        [Required]
        public List<string> TestTypes { get; set; } = new List<string>();
        
        [Required]
        public string Result { get; set; } = string.Empty;
    }
}
