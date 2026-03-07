using System.ComponentModel.DataAnnotations;

namespace IT15_DairyFlow.Models.Admin
{
    public class UserListItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Roles { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsProtected { get; set; }
    }

    public class CreateUserViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class EditUserViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string UserName { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }

        public bool IsActive { get; set; }
        public bool IsProtected { get; set; }
        public string RoleSummary { get; set; } = string.Empty;
    }

    public class RoleListItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class RoleEditViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;
    }

    public class UserRoleItemViewModel
    {
        public string RoleId { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public bool Selected { get; set; }
    }

    public class UserRolesViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<UserRoleItemViewModel> Roles { get; set; } = new();
    }

    public class AdminDashboardViewModel
    {
        // Base user stats (used by both Admin and Home/Index)
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int AdminUsers { get; set; }
        public int UsersWithRoles { get; set; }

        // Superadmin stats (used by Home/Index for Superadmin)
        public int TotalCompanies { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int TrialCompanies { get; set; }
        public int SystemInstances { get; set; }
        public decimal MonthlyRecurringRevenue { get; set; }

        // Company-based admin dashboard stats
        public string CompanyName { get; set; } = string.Empty;
        public int CompanyID { get; set; }
        
        // KPI Metrics (for admin dashboard)
        public KPIMetricsViewModel KPIMetrics { get; set; } = new();
        
        // Audit Logs (for admin dashboard)
        public List<AuditLogViewModel> RecentAuditLogs { get; set; } = new();

        // Chart data (for admin dashboard)
        public List<string> MonthlyBatchLabels { get; set; } = new();
        public List<int> MonthlyBatchCounts { get; set; } = new();
        public List<string> MonthlyRevenueLabels { get; set; } = new();
        public List<decimal> MonthlyRevenues { get; set; } = new();
        public List<decimal> MonthlyExpenses { get; set; } = new();
    }

    public class KPIMetricsViewModel
    {
        // User Management KPIs
        public int TotalActiveUsers { get; set; }
        public int TotalInactiveUsers { get; set; }
        public decimal UserGrowthPercentage { get; set; }

        // Product Management KPIs
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int DiscontinuedProducts { get; set; }

        // Production KPIs
        public int TotalProductionBatches { get; set; }
        public int CompletedBatches { get; set; }
        public int OngoingBatches { get; set; }
        public decimal ProductionCompletionRate { get; set; }

        // Quality Inspection KPIs
        public int TotalQualityInspections { get; set; }
        public int PassedInspections { get; set; }
        public int FailedInspections { get; set; }
        public decimal QualityPassRate { get; set; }

        // Inventory KPIs
        public int TotalInventoryItems { get; set; }
        public int LowStockItems { get; set; }
        public decimal AverageInventoryHealth { get; set; }

        // Financial KPIs
        public decimal TotalExpenses { get; set; }
        public decimal BudgetUtilization { get; set; }
        public decimal TotalRevenue { get; set; }

        // System KPIs
        public DateTime LastModifiedDate { get; set; }
        public int SystemActivityCount { get; set; }
    }

    public class AuditLogViewModel
    {
        public int AuditLogID { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public DateTime TimeStamp { get; set; }
        public string FormattedTimeStamp => TimeStamp.ToString("MMM dd, yyyy HH:mm:ss");
        public string ActionBadgeClass => GetActionBadgeClass(Action);

        private string GetActionBadgeClass(string action)
        {
            return action.ToLower() switch
            {
                "create" => "badge-success",
                "update" => "badge-info",
                "delete" => "badge-danger",
                "login" => "badge-primary",
                "logout" => "badge-secondary",
                _ => "badge-light"
            };
        }
    }

    public class AuditLogFilterViewModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        [MaxLength(100)]
        public string? ActionFilter { get; set; }
        [MaxLength(450)]
        public string? UserFilter { get; set; }
        public List<AuditLogViewModel> AuditLogs { get; set; } = new();
        public int TotalRecords { get; set; }
    }
}
