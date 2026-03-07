using System.ComponentModel.DataAnnotations;

namespace IT15_DairyFlow.Models.Admin
{
    // ─── DASHBOARD ────────────────────────────────────────────
    public class SuperAdminDashboardViewModel
    {
        // Company stats
        public int TotalCompanies { get; set; }
        public int ActiveCompanies { get; set; }
        public int TrialCompanies { get; set; }
        public int ActiveSubscriptions { get; set; }
        public decimal MonthlyRecurringRevenue { get; set; }

        // User stats
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int AdminUsers { get; set; }

        // Global KPIs
        public int TotalProducts { get; set; }
        public int TotalBatches { get; set; }
        public int CompletedBatches { get; set; }
        public int TotalInspections { get; set; }
        public int PassedInspections { get; set; }
        public int FailedInspections { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalInventoryItems { get; set; }

        // Chart data
        public List<string> MonthlyBatchLabels { get; set; } = new();
        public List<int> MonthlyBatchCounts { get; set; } = new();

        // Recent logs
        public List<SuperAdminAuditLogViewModel> RecentAuditLogs { get; set; } = new();
    }

    // ─── COMPANIES ────────────────────────────────────────────
    public class CompanyListItemViewModel
    {
        public int CompanyID { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string SubscriptionPlan { get; set; } = string.Empty;
        public string BillingCycle { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int UserCount { get; set; }
    }

    // ─── SUBSCRIPTIONS ────────────────────────────────────────
    public class SubscriptionListItemViewModel
    {
        public int SubscriptionID { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string BillingCycle { get; set; } = string.Empty;
        public int CompanyCount { get; set; }
    }

    // ─── ANALYTICS ────────────────────────────────────────────
    public class CompanyAnalyticsViewModel
    {
        public string CompanyName { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public int ProductCount { get; set; }
        public int BatchCount { get; set; }
        public int InspectionCount { get; set; }
        public int InventoryCount { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class AnalyticsPageViewModel
    {
        public List<CompanyAnalyticsViewModel> CompanyStats { get; set; } = new();
        public List<string> CompanyLabels { get; set; } = new();
        public List<int> CompanyUserCounts { get; set; } = new();
        public List<int> CompanyBatchCounts { get; set; } = new();
        public List<decimal> CompanyRevenues { get; set; } = new();
    }

    // ─── USERS ────────────────────────────────────────────────
    public class SuperAdminUserListItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Roles { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public int? CompanyID { get; set; }
    }

    // ─── AUDIT LOGS ───────────────────────────────────────────
    public class SuperAdminAuditLogViewModel
    {
        public int AuditLogID { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public DateTime TimeStamp { get; set; }
        public string FormattedTimeStamp => TimeStamp.ToString("MMM dd, yyyy HH:mm:ss");
    }

    public class SuperAdminLogFilterViewModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        [MaxLength(100)]
        public string? ActionFilter { get; set; }
        public List<SuperAdminAuditLogViewModel> AuditLogs { get; set; } = new();
        public int TotalRecords { get; set; }
    }
}
