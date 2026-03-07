using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT15_DairyFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class QMEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Companies_CompanyID",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_AspNetUsers_UserID",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Companies_CompanyID",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_BillingInvoices_Companies_CompanyID",
                table: "BillingInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Budgets_Companies_CompanyID",
                table: "Budgets");

            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Subscriptions_SubscriptionID",
                table: "Companies");

            migrationBuilder.DropForeignKey(
                name: "FK_Equipment_Companies_CompanyID",
                table: "Equipment");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_AspNetUsers_UserID",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Companies_CompanyID",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Suppliers_SupplierID",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_AspNetUsers_UserID",
                table: "Inventories");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Companies_CompanyID",
                table: "Inventories");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Product_ProductID",
                table: "Inventories");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntries_Companies_CompanyID",
                table: "JournalEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_Product_Companies_CompanyID",
                table: "Product");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionBatch_Companies_CompanyID",
                table: "ProductionBatch");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionCosts_Companies_CompanyID",
                table: "ProductionCosts");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionCosts_ProductionBatch_ProductionBatchID",
                table: "ProductionCosts");

            migrationBuilder.DropForeignKey(
                name: "FK_QualityInspections_AspNetUsers_UserId",
                table: "QualityInspections");

            migrationBuilder.DropForeignKey(
                name: "FK_QualityInspections_Companies_CompanyID",
                table: "QualityInspections");

            migrationBuilder.DropForeignKey(
                name: "FK_QualityInspections_ProductionBatch_ProductionBatchID",
                table: "QualityInspections");

            migrationBuilder.DropForeignKey(
                name: "FK_RawMaterials_Companies_CompanyID",
                table: "RawMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_RawMaterials_Suppliers_SupplierID",
                table: "RawMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_Companies_CompanyID",
                table: "Suppliers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Suppliers",
                table: "Suppliers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Subscriptions",
                table: "Subscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RawMaterials",
                table: "RawMaterials");

            migrationBuilder.DropPrimaryKey(
                name: "PK_QualityInspections",
                table: "QualityInspections");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductionCosts",
                table: "ProductionCosts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JournalEntries",
                table: "JournalEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Inventories",
                table: "Inventories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Expenses",
                table: "Expenses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Companies",
                table: "Companies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Budgets",
                table: "Budgets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BillingInvoices",
                table: "BillingInvoices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuditLogs",
                table: "AuditLogs");

            migrationBuilder.RenameTable(
                name: "Suppliers",
                newName: "Supplier");

            migrationBuilder.RenameTable(
                name: "Subscriptions",
                newName: "Subscription");

            migrationBuilder.RenameTable(
                name: "RawMaterials",
                newName: "RawMaterial");

            migrationBuilder.RenameTable(
                name: "QualityInspections",
                newName: "QualityInspection");

            migrationBuilder.RenameTable(
                name: "ProductionCosts",
                newName: "ProductionCost");

            migrationBuilder.RenameTable(
                name: "JournalEntries",
                newName: "JournalEntry");

            migrationBuilder.RenameTable(
                name: "Inventories",
                newName: "Inventory");

            migrationBuilder.RenameTable(
                name: "Expenses",
                newName: "Expense");

            migrationBuilder.RenameTable(
                name: "Companies",
                newName: "Company");

            migrationBuilder.RenameTable(
                name: "Budgets",
                newName: "Budget");

            migrationBuilder.RenameTable(
                name: "BillingInvoices",
                newName: "BillingInvoice");

            migrationBuilder.RenameTable(
                name: "AuditLogs",
                newName: "AuditLog");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Supplier",
                newName: "SupplierName");

            migrationBuilder.RenameIndex(
                name: "IX_Suppliers_CompanyID",
                table: "Supplier",
                newName: "IX_Supplier_CompanyID");

            migrationBuilder.RenameIndex(
                name: "IX_RawMaterials_SupplierID",
                table: "RawMaterial",
                newName: "IX_RawMaterial_SupplierID");

            migrationBuilder.RenameIndex(
                name: "IX_RawMaterials_CompanyID",
                table: "RawMaterial",
                newName: "IX_RawMaterial_CompanyID");

            migrationBuilder.RenameIndex(
                name: "IX_QualityInspections_UserId",
                table: "QualityInspection",
                newName: "IX_QualityInspection_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_QualityInspections_ProductionBatchID",
                table: "QualityInspection",
                newName: "IX_QualityInspection_ProductionBatchID");

            migrationBuilder.RenameIndex(
                name: "IX_QualityInspections_CompanyID",
                table: "QualityInspection",
                newName: "IX_QualityInspection_CompanyID");

            migrationBuilder.RenameIndex(
                name: "IX_ProductionCosts_ProductionBatchID",
                table: "ProductionCost",
                newName: "IX_ProductionCost_ProductionBatchID");

            migrationBuilder.RenameIndex(
                name: "IX_ProductionCosts_CompanyID",
                table: "ProductionCost",
                newName: "IX_ProductionCost_CompanyID");

            migrationBuilder.RenameIndex(
                name: "IX_JournalEntries_CompanyID",
                table: "JournalEntry",
                newName: "IX_JournalEntry_CompanyID");

            migrationBuilder.RenameIndex(
                name: "IX_Inventories_UserID",
                table: "Inventory",
                newName: "IX_Inventory_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_Inventories_ProductID",
                table: "Inventory",
                newName: "IX_Inventory_ProductID");

            migrationBuilder.RenameIndex(
                name: "IX_Inventories_CompanyID",
                table: "Inventory",
                newName: "IX_Inventory_CompanyID");

            migrationBuilder.RenameIndex(
                name: "IX_Expenses_UserID",
                table: "Expense",
                newName: "IX_Expense_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_Expenses_SupplierID",
                table: "Expense",
                newName: "IX_Expense_SupplierID");

            migrationBuilder.RenameIndex(
                name: "IX_Expenses_CompanyID",
                table: "Expense",
                newName: "IX_Expense_CompanyID");

            migrationBuilder.RenameIndex(
                name: "IX_Companies_SubscriptionID",
                table: "Company",
                newName: "IX_Company_SubscriptionID");

            migrationBuilder.RenameIndex(
                name: "IX_Budgets_CompanyID",
                table: "Budget",
                newName: "IX_Budget_CompanyID");

            migrationBuilder.RenameIndex(
                name: "IX_BillingInvoices_CompanyID",
                table: "BillingInvoice",
                newName: "IX_BillingInvoice_CompanyID");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLogs_UserID",
                table: "AuditLog",
                newName: "IX_AuditLog_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLogs_CompanyID",
                table: "AuditLog",
                newName: "IX_AuditLog_CompanyID");

            migrationBuilder.AddColumn<string>(
                name: "AcceptableRanges",
                table: "QualityInspection",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Acidity",
                table: "QualityInspection",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AntibioticTest",
                table: "QualityInspection",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BacterialCount",
                table: "QualityInspection",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedDate",
                table: "QualityInspection",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrectiveAction",
                table: "QualityInspection",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FatContent",
                table: "QualityInspection",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InspectionDate",
                table: "QualityInspection",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InspectionType",
                table: "QualityInspection",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MoistureContent",
                table: "QualityInspection",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "QualityInspection",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PHLevel",
                table: "QualityInspection",
                type: "decimal(4,2)",
                precision: 4,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProteinContent",
                table: "QualityInspection",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SampleLot",
                table: "QualityInspection",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SampleSize",
                table: "QualityInspection",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SomaticCellCount",
                table: "QualityInspection",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Temperature",
                table: "QualityInspection",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InvoiceDate",
                table: "BillingInvoice",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Supplier",
                table: "Supplier",
                column: "SupplierID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Subscription",
                table: "Subscription",
                column: "SubscriptionID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RawMaterial",
                table: "RawMaterial",
                column: "RawMaterialID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QualityInspection",
                table: "QualityInspection",
                column: "QualityInspectionID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductionCost",
                table: "ProductionCost",
                column: "ProductionCostID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JournalEntry",
                table: "JournalEntry",
                column: "JournalEntryID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Inventory",
                table: "Inventory",
                column: "InventoryID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Expense",
                table: "Expense",
                column: "ExpenseID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Company",
                table: "Company",
                column: "CompanyID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Budget",
                table: "Budget",
                column: "BudgetID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BillingInvoice",
                table: "BillingInvoice",
                column: "BillingInvoiceID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuditLog",
                table: "AuditLog",
                column: "AuditLogID");

            migrationBuilder.CreateTable(
                name: "NonConformance",
                columns: table => new
                {
                    NonConformanceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyID = table.Column<int>(type: "int", nullable: false),
                    QualityInspectionID = table.Column<int>(type: "int", nullable: true),
                    ProductionBatchID = table.Column<int>(type: "int", nullable: true),
                    ReportedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AssignedToUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    NCRNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RootCause = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ImmediateAction = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CorrectiveAction = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PreventiveAction = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AffectedQuantity = table.Column<int>(type: "int", nullable: true),
                    CostImpact = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Disposition = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReportedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NonConformance", x => x.NonConformanceID);
                    table.ForeignKey(
                        name: "FK_NonConformance_AspNetUsers_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_NonConformance_AspNetUsers_ReportedByUserId",
                        column: x => x.ReportedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NonConformance_AspNetUsers_VerifiedByUserId",
                        column: x => x.VerifiedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NonConformance_Company_CompanyID",
                        column: x => x.CompanyID,
                        principalTable: "Company",
                        principalColumn: "CompanyID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NonConformance_ProductionBatch_ProductionBatchID",
                        column: x => x.ProductionBatchID,
                        principalTable: "ProductionBatch",
                        principalColumn: "ProductionBatchID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_NonConformance_QualityInspection_QualityInspectionID",
                        column: x => x.QualityInspectionID,
                        principalTable: "QualityInspection",
                        principalColumn: "QualityInspectionID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ProductFormulation",
                columns: table => new
                {
                    FormulationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    CompanyID = table.Column<int>(type: "int", nullable: false),
                    RawMaterialID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProcessOrder = table.Column<int>(type: "int", nullable: true),
                    ProcessInstructions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductFormulation", x => x.FormulationID);
                    table.ForeignKey(
                        name: "FK_ProductFormulation_Company_CompanyID",
                        column: x => x.CompanyID,
                        principalTable: "Company",
                        principalColumn: "CompanyID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductFormulation_Product_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Product",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductFormulation_RawMaterial_RawMaterialID",
                        column: x => x.RawMaterialID,
                        principalTable: "RawMaterial",
                        principalColumn: "RawMaterialID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NonConformance_AssignedToUserId",
                table: "NonConformance",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NonConformance_CompanyID",
                table: "NonConformance",
                column: "CompanyID");

            migrationBuilder.CreateIndex(
                name: "IX_NonConformance_ProductionBatchID",
                table: "NonConformance",
                column: "ProductionBatchID");

            migrationBuilder.CreateIndex(
                name: "IX_NonConformance_QualityInspectionID",
                table: "NonConformance",
                column: "QualityInspectionID");

            migrationBuilder.CreateIndex(
                name: "IX_NonConformance_ReportedByUserId",
                table: "NonConformance",
                column: "ReportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NonConformance_VerifiedByUserId",
                table: "NonConformance",
                column: "VerifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductFormulation_CompanyID",
                table: "ProductFormulation",
                column: "CompanyID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductFormulation_ProductID",
                table: "ProductFormulation",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductFormulation_RawMaterialID",
                table: "ProductFormulation",
                column: "RawMaterialID");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Company_CompanyID",
                table: "AspNetUsers",
                column: "CompanyID",
                principalTable: "Company",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLog_AspNetUsers_UserID",
                table: "AuditLog",
                column: "UserID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLog_Company_CompanyID",
                table: "AuditLog",
                column: "CompanyID",
                principalTable: "Company",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BillingInvoice_Company_CompanyID",
                table: "BillingInvoice",
                column: "CompanyID",
                principalTable: "Company",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Budget_Company_CompanyID",
                table: "Budget",
                column: "CompanyID",
                principalTable: "Company",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Company_Subscription_SubscriptionID",
                table: "Company",
                column: "SubscriptionID",
                principalTable: "Subscription",
                principalColumn: "SubscriptionID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Equipment_Company_CompanyID",
                table: "Equipment",
                column: "CompanyID",
                principalTable: "Company",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Expense_AspNetUsers_UserID",
                table: "Expense",
                column: "UserID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Expense_Company_CompanyID",
                table: "Expense",
                column: "CompanyID",
                principalTable: "Company",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Expense_Supplier_SupplierID",
                table: "Expense",
                column: "SupplierID",
                principalTable: "Supplier",
                principalColumn: "SupplierID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventory_AspNetUsers_UserID",
                table: "Inventory",
                column: "UserID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventory_Company_CompanyID",
                table: "Inventory",
                column: "CompanyID",
                principalTable: "Company",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventory_Product_ProductID",
                table: "Inventory",
                column: "ProductID",
                principalTable: "Product",
                principalColumn: "ProductID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntry_Company_CompanyID",
                table: "JournalEntry",
                column: "CompanyID",
                principalTable: "Company",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Product_Company_CompanyID",
                table: "Product",
                column: "CompanyID",
                principalTable: "Company",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBatch_Company_CompanyID",
                table: "ProductionBatch",
                column: "CompanyID",
                principalTable: "Company",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionCost_Company_CompanyID",
                table: "ProductionCost",
                column: "CompanyID",
                principalTable: "Company",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionCost_ProductionBatch_ProductionBatchID",
                table: "ProductionCost",
                column: "ProductionBatchID",
                principalTable: "ProductionBatch",
                principalColumn: "ProductionBatchID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QualityInspection_AspNetUsers_UserId",
                table: "QualityInspection",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QualityInspection_Company_CompanyID",
                table: "QualityInspection",
                column: "CompanyID",
                principalTable: "Company",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QualityInspection_ProductionBatch_ProductionBatchID",
                table: "QualityInspection",
                column: "ProductionBatchID",
                principalTable: "ProductionBatch",
                principalColumn: "ProductionBatchID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RawMaterial_Company_CompanyID",
                table: "RawMaterial",
                column: "CompanyID",
                principalTable: "Company",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RawMaterial_Supplier_SupplierID",
                table: "RawMaterial",
                column: "SupplierID",
                principalTable: "Supplier",
                principalColumn: "SupplierID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Supplier_Company_CompanyID",
                table: "Supplier",
                column: "CompanyID",
                principalTable: "Company",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Company_CompanyID",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLog_AspNetUsers_UserID",
                table: "AuditLog");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLog_Company_CompanyID",
                table: "AuditLog");

            migrationBuilder.DropForeignKey(
                name: "FK_BillingInvoice_Company_CompanyID",
                table: "BillingInvoice");

            migrationBuilder.DropForeignKey(
                name: "FK_Budget_Company_CompanyID",
                table: "Budget");

            migrationBuilder.DropForeignKey(
                name: "FK_Company_Subscription_SubscriptionID",
                table: "Company");

            migrationBuilder.DropForeignKey(
                name: "FK_Equipment_Company_CompanyID",
                table: "Equipment");

            migrationBuilder.DropForeignKey(
                name: "FK_Expense_AspNetUsers_UserID",
                table: "Expense");

            migrationBuilder.DropForeignKey(
                name: "FK_Expense_Company_CompanyID",
                table: "Expense");

            migrationBuilder.DropForeignKey(
                name: "FK_Expense_Supplier_SupplierID",
                table: "Expense");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventory_AspNetUsers_UserID",
                table: "Inventory");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventory_Company_CompanyID",
                table: "Inventory");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventory_Product_ProductID",
                table: "Inventory");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntry_Company_CompanyID",
                table: "JournalEntry");

            migrationBuilder.DropForeignKey(
                name: "FK_Product_Company_CompanyID",
                table: "Product");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionBatch_Company_CompanyID",
                table: "ProductionBatch");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionCost_Company_CompanyID",
                table: "ProductionCost");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionCost_ProductionBatch_ProductionBatchID",
                table: "ProductionCost");

            migrationBuilder.DropForeignKey(
                name: "FK_QualityInspection_AspNetUsers_UserId",
                table: "QualityInspection");

            migrationBuilder.DropForeignKey(
                name: "FK_QualityInspection_Company_CompanyID",
                table: "QualityInspection");

            migrationBuilder.DropForeignKey(
                name: "FK_QualityInspection_ProductionBatch_ProductionBatchID",
                table: "QualityInspection");

            migrationBuilder.DropForeignKey(
                name: "FK_RawMaterial_Company_CompanyID",
                table: "RawMaterial");

            migrationBuilder.DropForeignKey(
                name: "FK_RawMaterial_Supplier_SupplierID",
                table: "RawMaterial");

            migrationBuilder.DropForeignKey(
                name: "FK_Supplier_Company_CompanyID",
                table: "Supplier");

            migrationBuilder.DropTable(
                name: "NonConformance");

            migrationBuilder.DropTable(
                name: "ProductFormulation");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Supplier",
                table: "Supplier");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Subscription",
                table: "Subscription");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RawMaterial",
                table: "RawMaterial");

            migrationBuilder.DropPrimaryKey(
                name: "PK_QualityInspection",
                table: "QualityInspection");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductionCost",
                table: "ProductionCost");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JournalEntry",
                table: "JournalEntry");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Inventory",
                table: "Inventory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Expense",
                table: "Expense");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Company",
                table: "Company");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Budget",
                table: "Budget");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BillingInvoice",
                table: "BillingInvoice");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuditLog",
                table: "AuditLog");

            migrationBuilder.DropColumn(
                name: "AcceptableRanges",
                table: "QualityInspection");

            migrationBuilder.DropColumn(
                name: "Acidity",
                table: "QualityInspection");

            migrationBuilder.DropColumn(
                name: "AntibioticTest",
                table: "QualityInspection");

            migrationBuilder.DropColumn(
                name: "BacterialCount",
                table: "QualityInspection");

            migrationBuilder.DropColumn(
                name: "CompletedDate",
                table: "QualityInspection");

            migrationBuilder.DropColumn(
                name: "CorrectiveAction",
                table: "QualityInspection");

            migrationBuilder.DropColumn(
                name: "FatContent",
                table: "QualityInspection");

            migrationBuilder.DropColumn(
                name: "InspectionDate",
                table: "QualityInspection");

            migrationBuilder.DropColumn(
                name: "InspectionType",
                table: "QualityInspection");

            migrationBuilder.DropColumn(
                name: "MoistureContent",
                table: "QualityInspection");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "QualityInspection");

            migrationBuilder.DropColumn(
                name: "PHLevel",
                table: "QualityInspection");

            migrationBuilder.DropColumn(
                name: "ProteinContent",
                table: "QualityInspection");

            migrationBuilder.DropColumn(
                name: "SampleLot",
                table: "QualityInspection");

            migrationBuilder.DropColumn(
                name: "SampleSize",
                table: "QualityInspection");

            migrationBuilder.DropColumn(
                name: "SomaticCellCount",
                table: "QualityInspection");

            migrationBuilder.DropColumn(
                name: "Temperature",
                table: "QualityInspection");

            migrationBuilder.DropColumn(
                name: "InvoiceDate",
                table: "BillingInvoice");

            migrationBuilder.RenameTable(
                name: "Supplier",
                newName: "Suppliers");

            migrationBuilder.RenameTable(
                name: "Subscription",
                newName: "Subscriptions");

            migrationBuilder.RenameTable(
                name: "RawMaterial",
                newName: "RawMaterials");

            migrationBuilder.RenameTable(
                name: "QualityInspection",
                newName: "QualityInspections");

            migrationBuilder.RenameTable(
                name: "ProductionCost",
                newName: "ProductionCosts");

            migrationBuilder.RenameTable(
                name: "JournalEntry",
                newName: "JournalEntries");

            migrationBuilder.RenameTable(
                name: "Inventory",
                newName: "Inventories");

            migrationBuilder.RenameTable(
                name: "Expense",
                newName: "Expenses");

            migrationBuilder.RenameTable(
                name: "Company",
                newName: "Companies");

            migrationBuilder.RenameTable(
                name: "Budget",
                newName: "Budgets");

            migrationBuilder.RenameTable(
                name: "BillingInvoice",
                newName: "BillingInvoices");

            migrationBuilder.RenameTable(
                name: "AuditLog",
                newName: "AuditLogs");

            migrationBuilder.RenameColumn(
                name: "SupplierName",
                table: "Suppliers",
                newName: "Name");

            migrationBuilder.RenameIndex(
                name: "IX_Supplier_CompanyID",
                table: "Suppliers",
                newName: "IX_Suppliers_CompanyID");

            migrationBuilder.RenameIndex(
                name: "IX_RawMaterial_SupplierID",
                table: "RawMaterials",
                newName: "IX_RawMaterials_SupplierID");

            migrationBuilder.RenameIndex(
                name: "IX_RawMaterial_CompanyID",
                table: "RawMaterials",
                newName: "IX_RawMaterials_CompanyID");

            migrationBuilder.RenameIndex(
                name: "IX_QualityInspection_UserId",
                table: "QualityInspections",
                newName: "IX_QualityInspections_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_QualityInspection_ProductionBatchID",
                table: "QualityInspections",
                newName: "IX_QualityInspections_ProductionBatchID");

            migrationBuilder.RenameIndex(
                name: "IX_QualityInspection_CompanyID",
                table: "QualityInspections",
                newName: "IX_QualityInspections_CompanyID");

            migrationBuilder.RenameIndex(
                name: "IX_ProductionCost_ProductionBatchID",
                table: "ProductionCosts",
                newName: "IX_ProductionCosts_ProductionBatchID");

            migrationBuilder.RenameIndex(
                name: "IX_ProductionCost_CompanyID",
                table: "ProductionCosts",
                newName: "IX_ProductionCosts_CompanyID");

            migrationBuilder.RenameIndex(
                name: "IX_JournalEntry_CompanyID",
                table: "JournalEntries",
                newName: "IX_JournalEntries_CompanyID");

            migrationBuilder.RenameIndex(
                name: "IX_Inventory_UserID",
                table: "Inventories",
                newName: "IX_Inventories_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_Inventory_ProductID",
                table: "Inventories",
                newName: "IX_Inventories_ProductID");

            migrationBuilder.RenameIndex(
                name: "IX_Inventory_CompanyID",
                table: "Inventories",
                newName: "IX_Inventories_CompanyID");

            migrationBuilder.RenameIndex(
                name: "IX_Expense_UserID",
                table: "Expenses",
                newName: "IX_Expenses_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_Expense_SupplierID",
                table: "Expenses",
                newName: "IX_Expenses_SupplierID");

            migrationBuilder.RenameIndex(
                name: "IX_Expense_CompanyID",
                table: "Expenses",
                newName: "IX_Expenses_CompanyID");

            migrationBuilder.RenameIndex(
                name: "IX_Company_SubscriptionID",
                table: "Companies",
                newName: "IX_Companies_SubscriptionID");

            migrationBuilder.RenameIndex(
                name: "IX_Budget_CompanyID",
                table: "Budgets",
                newName: "IX_Budgets_CompanyID");

            migrationBuilder.RenameIndex(
                name: "IX_BillingInvoice_CompanyID",
                table: "BillingInvoices",
                newName: "IX_BillingInvoices_CompanyID");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLog_UserID",
                table: "AuditLogs",
                newName: "IX_AuditLogs_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLog_CompanyID",
                table: "AuditLogs",
                newName: "IX_AuditLogs_CompanyID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Suppliers",
                table: "Suppliers",
                column: "SupplierID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Subscriptions",
                table: "Subscriptions",
                column: "SubscriptionID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RawMaterials",
                table: "RawMaterials",
                column: "RawMaterialID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QualityInspections",
                table: "QualityInspections",
                column: "QualityInspectionID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductionCosts",
                table: "ProductionCosts",
                column: "ProductionCostID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JournalEntries",
                table: "JournalEntries",
                column: "JournalEntryID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Inventories",
                table: "Inventories",
                column: "InventoryID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Expenses",
                table: "Expenses",
                column: "ExpenseID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Companies",
                table: "Companies",
                column: "CompanyID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Budgets",
                table: "Budgets",
                column: "BudgetID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BillingInvoices",
                table: "BillingInvoices",
                column: "BillingInvoiceID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuditLogs",
                table: "AuditLogs",
                column: "AuditLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Companies_CompanyID",
                table: "AspNetUsers",
                column: "CompanyID",
                principalTable: "Companies",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_AspNetUsers_UserID",
                table: "AuditLogs",
                column: "UserID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Companies_CompanyID",
                table: "AuditLogs",
                column: "CompanyID",
                principalTable: "Companies",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BillingInvoices_Companies_CompanyID",
                table: "BillingInvoices",
                column: "CompanyID",
                principalTable: "Companies",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Budgets_Companies_CompanyID",
                table: "Budgets",
                column: "CompanyID",
                principalTable: "Companies",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Subscriptions_SubscriptionID",
                table: "Companies",
                column: "SubscriptionID",
                principalTable: "Subscriptions",
                principalColumn: "SubscriptionID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Equipment_Companies_CompanyID",
                table: "Equipment",
                column: "CompanyID",
                principalTable: "Companies",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_AspNetUsers_UserID",
                table: "Expenses",
                column: "UserID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Companies_CompanyID",
                table: "Expenses",
                column: "CompanyID",
                principalTable: "Companies",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Suppliers_SupplierID",
                table: "Expenses",
                column: "SupplierID",
                principalTable: "Suppliers",
                principalColumn: "SupplierID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_AspNetUsers_UserID",
                table: "Inventories",
                column: "UserID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Companies_CompanyID",
                table: "Inventories",
                column: "CompanyID",
                principalTable: "Companies",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Product_ProductID",
                table: "Inventories",
                column: "ProductID",
                principalTable: "Product",
                principalColumn: "ProductID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntries_Companies_CompanyID",
                table: "JournalEntries",
                column: "CompanyID",
                principalTable: "Companies",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Product_Companies_CompanyID",
                table: "Product",
                column: "CompanyID",
                principalTable: "Companies",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBatch_Companies_CompanyID",
                table: "ProductionBatch",
                column: "CompanyID",
                principalTable: "Companies",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionCosts_Companies_CompanyID",
                table: "ProductionCosts",
                column: "CompanyID",
                principalTable: "Companies",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionCosts_ProductionBatch_ProductionBatchID",
                table: "ProductionCosts",
                column: "ProductionBatchID",
                principalTable: "ProductionBatch",
                principalColumn: "ProductionBatchID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QualityInspections_AspNetUsers_UserId",
                table: "QualityInspections",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QualityInspections_Companies_CompanyID",
                table: "QualityInspections",
                column: "CompanyID",
                principalTable: "Companies",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QualityInspections_ProductionBatch_ProductionBatchID",
                table: "QualityInspections",
                column: "ProductionBatchID",
                principalTable: "ProductionBatch",
                principalColumn: "ProductionBatchID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RawMaterials_Companies_CompanyID",
                table: "RawMaterials",
                column: "CompanyID",
                principalTable: "Companies",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RawMaterials_Suppliers_SupplierID",
                table: "RawMaterials",
                column: "SupplierID",
                principalTable: "Suppliers",
                principalColumn: "SupplierID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_Companies_CompanyID",
                table: "Suppliers",
                column: "CompanyID",
                principalTable: "Companies",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
