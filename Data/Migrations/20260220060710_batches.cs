using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT15_DairyFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class batches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Companies",
                keyColumn: "CompanyID",
                keyValue: 1);

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "ProductionBatches",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "BatchCode",
                table: "ProductionBatches",
                newName: "batchCode");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "ProductionBatches",
                newName: "StartDate");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "ProductionBatches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EquipmentID",
                table: "ProductionBatches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductID",
                table: "ProductionBatches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UserID",
                table: "ProductionBatches",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CompanyID",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Equipment",
                columns: table => new
                {
                    EquipmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyID = table.Column<int>(type: "int", nullable: false),
                    EquipmentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EquipmentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastMaintenanceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipment", x => x.EquipmentID);
                    table.ForeignKey(
                        name: "FK_Equipment_Companies_CompanyID",
                        column: x => x.CompanyID,
                        principalTable: "Companies",
                        principalColumn: "CompanyID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatches_EquipmentID",
                table: "ProductionBatches",
                column: "EquipmentID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatches_ProductID",
                table: "ProductionBatches",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatches_UserID",
                table: "ProductionBatches",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CompanyID",
                table: "AspNetUsers",
                column: "CompanyID");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_CompanyID",
                table: "Equipment",
                column: "CompanyID");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Companies_CompanyID",
                table: "AspNetUsers",
                column: "CompanyID",
                principalTable: "Companies",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBatches_AspNetUsers_UserID",
                table: "ProductionBatches",
                column: "UserID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBatches_Equipment_EquipmentID",
                table: "ProductionBatches",
                column: "EquipmentID",
                principalTable: "Equipment",
                principalColumn: "EquipmentID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBatches_Product_ProductID",
                table: "ProductionBatches",
                column: "ProductID",
                principalTable: "Product",
                principalColumn: "ProductID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Companies_CompanyID",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionBatches_AspNetUsers_UserID",
                table: "ProductionBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionBatches_Equipment_EquipmentID",
                table: "ProductionBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionBatches_Product_ProductID",
                table: "ProductionBatches");

            migrationBuilder.DropTable(
                name: "Equipment");

            migrationBuilder.DropIndex(
                name: "IX_ProductionBatches_EquipmentID",
                table: "ProductionBatches");

            migrationBuilder.DropIndex(
                name: "IX_ProductionBatches_ProductID",
                table: "ProductionBatches");

            migrationBuilder.DropIndex(
                name: "IX_ProductionBatches_UserID",
                table: "ProductionBatches");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CompanyID",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "ProductionBatches");

            migrationBuilder.DropColumn(
                name: "EquipmentID",
                table: "ProductionBatches");

            migrationBuilder.DropColumn(
                name: "ProductID",
                table: "ProductionBatches");

            migrationBuilder.DropColumn(
                name: "UserID",
                table: "ProductionBatches");

            migrationBuilder.DropColumn(
                name: "CompanyID",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "ProductionBatches",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "batchCode",
                table: "ProductionBatches",
                newName: "BatchCode");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "ProductionBatches",
                newName: "Date");

            migrationBuilder.InsertData(
                table: "Companies",
                columns: new[] { "CompanyID", "CompanyName", "Status", "SubscriptionID" },
                values: new object[] { 1, "Unassigned", "Unassigned", null });
        }
    }
}
