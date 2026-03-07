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

        // User stats (aggregated only — no per-user data)
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }

        // Per-company user adoption chart data
        public List<CompanyAnalyticsViewModel> CompanyUserStats { get; set; } = new();
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
        public string SubscriptionPlan { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class AnalyticsPageViewModel
    {
        public List<CompanyAnalyticsViewModel> CompanyStats { get; set; } = new();
        public List<string> CompanyLabels { get; set; } = new();
        public List<int> CompanyUserCounts { get; set; } = new();
    }
}
