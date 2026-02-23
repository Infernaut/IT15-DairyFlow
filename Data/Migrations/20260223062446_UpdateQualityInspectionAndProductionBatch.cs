using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT15_DairyFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateQualityInspectionAndProductionBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductionBatches_AspNetUsers_UserID",
                table: "ProductionBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionBatches_Companies_CompanyID",
                table: "ProductionBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionBatches_Equipment_EquipmentID",
                table: "ProductionBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionBatches_Product_ProductID",
                table: "ProductionBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionCosts_ProductionBatches_ProductionBatchID",
                table: "ProductionCosts");

            migrationBuilder.DropForeignKey(
                name: "FK_QualityInspections_ProductionBatches_ProductionBatchID",
                table: "QualityInspections");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductionBatches",
                table: "ProductionBatches");

            migrationBuilder.RenameTable(
                name: "ProductionBatches",
                newName: "ProductionBatch");

            migrationBuilder.RenameIndex(
                name: "IX_ProductionBatches_UserID",
                table: "ProductionBatch",
                newName: "IX_ProductionBatch_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_ProductionBatches_ProductID",
                table: "ProductionBatch",
                newName: "IX_ProductionBatch_ProductID");

            migrationBuilder.RenameIndex(
                name: "IX_ProductionBatches_EquipmentID",
                table: "ProductionBatch",
                newName: "IX_ProductionBatch_EquipmentID");

            migrationBuilder.RenameIndex(
                name: "IX_ProductionBatches_CompanyID",
                table: "ProductionBatch",
                newName: "IX_ProductionBatch_CompanyID");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "QualityInspections",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "QualityInspections",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "QualityInspections",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "ProductionBatch",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "quantity",
                table: "ProductionBatch",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductionBatch",
                table: "ProductionBatch",
                column: "ProductionBatchID");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_UserId",
                table: "QualityInspections",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBatch_AspNetUsers_UserID",
                table: "ProductionBatch",
                column: "UserID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBatch_Companies_CompanyID",
                table: "ProductionBatch",
                column: "CompanyID",
                principalTable: "Companies",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBatch_Equipment_EquipmentID",
                table: "ProductionBatch",
                column: "EquipmentID",
                principalTable: "Equipment",
                principalColumn: "EquipmentID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBatch_Product_ProductID",
                table: "ProductionBatch",
                column: "ProductID",
                principalTable: "Product",
                principalColumn: "ProductID",
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
                name: "FK_QualityInspections_ProductionBatch_ProductionBatchID",
                table: "QualityInspections",
                column: "ProductionBatchID",
                principalTable: "ProductionBatch",
                principalColumn: "ProductionBatchID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductionBatch_AspNetUsers_UserID",
                table: "ProductionBatch");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionBatch_Companies_CompanyID",
                table: "ProductionBatch");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionBatch_Equipment_EquipmentID",
                table: "ProductionBatch");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionBatch_Product_ProductID",
                table: "ProductionBatch");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionCosts_ProductionBatch_ProductionBatchID",
                table: "ProductionCosts");

            migrationBuilder.DropForeignKey(
                name: "FK_QualityInspections_AspNetUsers_UserId",
                table: "QualityInspections");

            migrationBuilder.DropForeignKey(
                name: "FK_QualityInspections_ProductionBatch_ProductionBatchID",
                table: "QualityInspections");

            migrationBuilder.DropIndex(
                name: "IX_QualityInspections_UserId",
                table: "QualityInspections");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductionBatch",
                table: "ProductionBatch");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "QualityInspections");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "QualityInspections");

            migrationBuilder.DropColumn(
                name: "quantity",
                table: "ProductionBatch");

            migrationBuilder.RenameTable(
                name: "ProductionBatch",
                newName: "ProductionBatches");

            migrationBuilder.RenameIndex(
                name: "IX_ProductionBatch_UserID",
                table: "ProductionBatches",
                newName: "IX_ProductionBatches_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_ProductionBatch_ProductID",
                table: "ProductionBatches",
                newName: "IX_ProductionBatches_ProductID");

            migrationBuilder.RenameIndex(
                name: "IX_ProductionBatch_EquipmentID",
                table: "ProductionBatches",
                newName: "IX_ProductionBatches_EquipmentID");

            migrationBuilder.RenameIndex(
                name: "IX_ProductionBatch_CompanyID",
                table: "ProductionBatches",
                newName: "IX_ProductionBatches_CompanyID");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "QualityInspections",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "ProductionBatches",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductionBatches",
                table: "ProductionBatches",
                column: "ProductionBatchID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBatches_AspNetUsers_UserID",
                table: "ProductionBatches",
                column: "UserID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBatches_Companies_CompanyID",
                table: "ProductionBatches",
                column: "CompanyID",
                principalTable: "Companies",
                principalColumn: "CompanyID",
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

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionCosts_ProductionBatches_ProductionBatchID",
                table: "ProductionCosts",
                column: "ProductionBatchID",
                principalTable: "ProductionBatches",
                principalColumn: "ProductionBatchID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QualityInspections_ProductionBatches_ProductionBatchID",
                table: "QualityInspections",
                column: "ProductionBatchID",
                principalTable: "ProductionBatches",
                principalColumn: "ProductionBatchID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
