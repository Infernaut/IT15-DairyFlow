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
        public decimal AnnualRecurringRevenue { get; set; }

        // Revenue stats (from billing invoices)
        public decimal MonthlyInvoiceRevenue { get; set; }
        public decimal AnnualInvoiceRevenue { get; set; }

        // User stats (aggregated only — no per-user data)
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }

        // Per-company user adoption chart data
        public List<CompanyAnalyticsViewModel> CompanyUserStats { get; set; } = new();

        // Monthly revenue chart data
        public List<string> MonthlyRevenueLabels { get; set; } = new();
        public List<decimal> MonthlyRevenueData { get; set; } = new();
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
        public int? RemainingDays { get; set; }
        public DateTime? SubscriptionStartDate { get; set; }
    }

    // ─── COMPANIES PAGE VIEW MODEL (wraps list + add form) ───
    public class CompaniesPageViewModel
    {
        public List<CompanyListItemViewModel> Companies { get; set; } = new();
        public AddCompanyViewModel AddCompany { get; set; } = new();
        public List<SubscriptionOptionItem> SubscriptionOptions { get; set; } = new();
    }

    // ─── ADD COMPANY ──────────────────────────────────────────
    public class AddCompanyViewModel
    {
        [Required(ErrorMessage = "Company name is required.")]
        [MaxLength(256)]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Admin email is required.")]
        [EmailAddress]
        [Display(Name = "Admin Email")]
        public string AdminEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username is required.")]
        [MaxLength(50)]
        [Display(Name = "Admin Username")]
        public string AdminUserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "Subscription Plan")]
        public int? SubscriptionID { get; set; }

        [Display(Name = "Company Status")]
        public string Status { get; set; } = "Active";
    }

    public class SubscriptionOptionItem
    {
        public int SubscriptionID { get; set; }
        public string DisplayName { get; set; } = string.Empty;
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
