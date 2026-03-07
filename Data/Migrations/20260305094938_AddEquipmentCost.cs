using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT15_DairyFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SupplierName",
                table: "Supplier",
                newName: "Name");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "RawMaterial",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CurrentStock",
                table: "RawMaterial",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRestockDate",
                table: "RawMaterial",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinimumStock",
                table: "RawMaterial",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "RawMaterial",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Cost",
                table: "Equipment",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "RawMaterial");

            migrationBuilder.DropColumn(
                name: "CurrentStock",
                table: "RawMaterial");

            migrationBuilder.DropColumn(
                name: "LastRestockDate",
                table: "RawMaterial");

            migrationBuilder.DropColumn(
                name: "MinimumStock",
                table: "RawMaterial");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "RawMaterial");

            migrationBuilder.DropColumn(
                name: "Cost",
                table: "Equipment");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Supplier",
                newName: "SupplierName");
        }
    }
}
