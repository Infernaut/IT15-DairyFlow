namespace IT15_DairyFlow.Security
{
    /// <summary>
    /// Central role names used across the ERP. Keep these stable (they may appear in the DB).
    /// </summary>
    public static class AppRoles
    {
        public const string Superadmin = "Superadmin";
        public const string CompanyAdmin = "Admin";

        public const string ProductManager = "ProductManager";
        public const string ProductionStaff = "ProductionStaff";
        public const string QualityOfficer = "QualityChecker";
        public const string InventoryManager = "InventoryManager";
        public const string SalesStaff = "Sales";
        public const string FinanceOfficer = "Finance";

        public const string AnyCompanyUser = CompanyAdmin + "," + ProductManager + "," + ProductionStaff + "," + QualityOfficer + "," + InventoryManager + "," + SalesStaff + "," + FinanceOfficer;

        public static readonly string[] All = new[]
        {
            Superadmin,
            CompanyAdmin,
            ProductManager,
            ProductionStaff,
            QualityOfficer,
            InventoryManager,
            SalesStaff,
            FinanceOfficer
        };
    }
}
