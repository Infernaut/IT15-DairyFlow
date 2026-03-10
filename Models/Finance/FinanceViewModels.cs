using System.ComponentModel.DataAnnotations;

namespace IT15_DairyFlow.Models.Finance
{
    public class FinanceDashboardViewModel
    {
        public FinancialSummaryViewModel Summary { get; set; } = new FinancialSummaryViewModel();
        public List<ExpenseListViewModel> RecentExpenses { get; set; } = new List<ExpenseListViewModel>();
        public List<BudgetListViewModel> Budgets { get; set; } = new List<BudgetListViewModel>();
        public List<BillingInvoiceListViewModel> RecentInvoices { get; set; } = new List<BillingInvoiceListViewModel>();
        public List<ProductionCostViewModel> ProductionCosts { get; set; } = new List<ProductionCostViewModel>();
        public List<SupplierLookupViewModel> Suppliers { get; set; } = new List<SupplierLookupViewModel>();
        public List<MonthlyBudgetStatusViewModel> MonthlyBudgets { get; set; } = new List<MonthlyBudgetStatusViewModel>();
        public string CurrentPeriod { get; set; } = string.Empty;
    }

    public class FinancialSummaryViewModel
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetIncome { get; set; }
        public decimal TotalBudget { get; set; }
        public decimal BudgetUtilization { get; set; }
        public decimal ProductionCosts { get; set; }
        public decimal PendingInvoices { get; set; }
        public int ExpenseCount { get; set; }
        public int InvoiceCount { get; set; }
    }

    public class ExpenseListViewModel
    {
        public int ExpenseID { get; set; }
        public decimal Amount { get; set; }
        public DateTime? ExpenseDate { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsEmergency { get; set; }
        public string? EmergencyReason { get; set; }
    }

    public class ExpenseDetailViewModel
    {
        public int ExpenseID { get; set; }
        public decimal Amount { get; set; }
        public DateTime? ExpenseDate { get; set; }
        public int? SupplierID { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserID { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsEmergency { get; set; }
        public string? EmergencyReason { get; set; }
    }

    public class CreateExpenseViewModel
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        public DateTime? ExpenseDate { get; set; }

        public int? SupplierID { get; set; }

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = "Other";

        /// <summary>
        /// Set to true to override the budget limit (emergency spending)
        /// </summary>
        public bool IsEmergency { get; set; } = false;

        [MaxLength(500)]
        public string? EmergencyReason { get; set; }
    }

    public class UpdateExpenseViewModel
    {
        [Required]
        public int ExpenseID { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        public DateTime? ExpenseDate { get; set; }

        public int? SupplierID { get; set; }

        [MaxLength(50)]
        public string Category { get; set; } = "Other";
    }

    public class BudgetListViewModel
    {
        public int BudgetID { get; set; }
        public string Period { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal AllocatedAmount { get; set; }
        public decimal SpentAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public bool IsOverBudget { get; set; }
        public int EmergencyCount { get; set; }
    }

    public class CreateBudgetViewModel
    {
        [Required]
        [MaxLength(50)]
        public string Period { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = "Other";

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal AllocatedAmount { get; set; }
    }

    public class UpdateBudgetViewModel
    {
        [Required]
        public int BudgetID { get; set; }

        [Required]
        [MaxLength(50)]
        public string Period { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = "Other";

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal AllocatedAmount { get; set; }
    }

    /// <summary>
    /// Shows budget status per category for the current month
    /// </summary>
    public class MonthlyBudgetStatusViewModel
    {
        public int BudgetID { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
        public decimal AllocatedAmount { get; set; }
        public decimal SpentAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public bool IsOverBudget { get; set; }
        public int EmergencyCount { get; set; }
        public decimal EmergencyTotal { get; set; }
    }

    /// <summary>
    /// Used by the CheckBudget endpoint to return limit status before creating expense
    /// </summary>
    public class BudgetCheckResult
    {
        public bool HasBudget { get; set; }
        public bool IsOverLimit { get; set; }
        public decimal AllocatedAmount { get; set; }
        public decimal SpentAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
    }

    public class BillingInvoiceListViewModel
    {
        public int BillingInvoiceID { get; set; }
        public decimal Amount { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
    }

    public class CreateBillingInvoiceViewModel
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        public DateTime? InvoiceDate { get; set; }

        public DateTime? DueDate { get; set; }

        [MaxLength(50)]
        public string PaymentStatus { get; set; } = "Pending";

        [MaxLength(200)]
        public string? CustomerName { get; set; }
    }

    public class UpdateBillingInvoiceViewModel
    {
        [Required]
        public int BillingInvoiceID { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        public DateTime? DueDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string PaymentStatus { get; set; } = string.Empty;
    }

    public class ProductionCostViewModel
    {
        public int ProductionCostID { get; set; }
        public int ProductionBatchID { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public decimal TotalCost { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class SupplierLookupViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class FinancialReportViewModel
    {
        public string ReportPeriod { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetProfit { get; set; }
        public List<ExpenseByCategoryViewModel> ExpensesByCategory { get; set; } = new List<ExpenseByCategoryViewModel>();
        public List<MonthlyFinancialSummary> MonthlySummary { get; set; } = new List<MonthlyFinancialSummary>();
    }

    public class ExpenseByCategoryViewModel
    {
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class MonthlyFinancialSummary
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal Expenses { get; set; }
        public decimal NetIncome { get; set; }
    }
}
