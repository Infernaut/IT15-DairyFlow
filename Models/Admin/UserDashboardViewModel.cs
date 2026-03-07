namespace IT15_DairyFlow.Models.Admin
{
    public class UserDashboardViewModel
    {
        // KPI Cards
        public int ActiveProducts { get; set; }
        public decimal ActiveProductGrowth { get; set; }
        public int OngoingBatches { get; set; }
        public int BatchesEndingToday { get; set; }
        public int TotalInventoryItems { get; set; }
        public int LowStockItems { get; set; }
        public string StockHealthLabel { get; set; } = "Healthy";
        public decimal ProductionCostMTD { get; set; }
        public decimal BudgetVarianceMTD { get; set; }

        // Production Volume Chart (6 months)
        public List<string> ProductionLabels { get; set; } = new();
        public List<int> ProductionActual { get; set; } = new();
        public List<int> ProductionTarget { get; set; } = new();

        // Quality Assurance
        public int QualityTotal { get; set; }
        public int QualityPassed { get; set; }
        public int QualityFailed { get; set; }
        public int QualityPending { get; set; }
        public decimal QualityPassRate { get; set; }

        // Inventory Alerts (upcoming expiry + low stock)
        public List<InventoryAlertItem> InventoryAlerts { get; set; } = new();

        // Cost Breakdown
        public decimal CostRawMaterials { get; set; }
        public decimal CostEquipment { get; set; }
        public decimal CostOther { get; set; }
        public decimal CostTotal { get; set; }
        public decimal CostRawMaterialsPct { get; set; }
        public decimal CostEquipmentPct { get; set; }
        public decimal CostOtherPct { get; set; }

        // Recent Audit Trail
        public List<DashboardAuditItem> RecentAuditLogs { get; set; } = new();

        // Revenue summary
        public decimal TotalRevenue { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetProfit => TotalRevenue - TotalExpenses;
    }

    public class InventoryAlertItem
    {
        public string ProductName { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public string AlertType { get; set; } = "warning"; // "danger" or "warning"
        public string AlertMessage { get; set; } = string.Empty;
    }

    public class DashboardAuditItem
    {
        public string Activity { get; set; } = string.Empty;
        public string Staff { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string TimeAgo { get; set; } = string.Empty;
        public string StatusLabel { get; set; } = string.Empty;
        public string StatusClass { get; set; } = "bg-soft-primary text-primary";
    }
}
