using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT15_DairyFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class zero : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Products_ProductID",
                table: "Inventories");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_AspNetUsers_UserID",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Companies_CompanyID",
                table: "Products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Products",
                table: "Products");

            migrationBuilder.RenameTable(
                name: "Products",
                newName: "Product");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Product",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "ProductName",
                table: "Product",
                newName: "productName");

            migrationBuilder.RenameColumn(
                name: "LifecycleStatus",
                table: "Product",
                newName: "lifecycleStatus");

            migrationBuilder.RenameIndex(
                name: "IX_Products_UserID",
                table: "Product",
                newName: "IX_Product_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_Products_CompanyID",
                table: "Product",
                newName: "IX_Product_CompanyID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Product",
                table: "Product",
                column: "ProductID");

            migrationBuilder.InsertData(
                table: "Companies",
                columns: new[] { "CompanyID", "CompanyName", "Status", "SubscriptionID" },
                values: new object[] { 1, "Unassigned", "Unassigned", null });

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Product_ProductID",
                table: "Inventories",
                column: "ProductID",
                principalTable: "Product",
                principalColumn: "ProductID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Product_AspNetUsers_UserID",
                table: "Product",
                column: "UserID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Product_Companies_CompanyID",
                table: "Product",
                column: "CompanyID",
                principalTable: "Companies",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Product_ProductID",
                table: "Inventories");

            migrationBuilder.DropForeignKey(
                name: "FK_Product_AspNetUsers_UserID",
                table: "Product");

            migrationBuilder.DropForeignKey(
                name: "FK_Product_Companies_CompanyID",
                table: "Product");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Product",
                table: "Product");

            migrationBuilder.DeleteData(
                table: "Companies",
                keyColumn: "CompanyID",
                keyValue: 1);

            migrationBuilder.RenameTable(
                name: "Product",
                newName: "Products");

            migrationBuilder.RenameColumn(
                name: "type",
                table: "Products",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "productName",
                table: "Products",
                newName: "ProductName");

            migrationBuilder.RenameColumn(
                name: "lifecycleStatus",
                table: "Products",
                newName: "LifecycleStatus");

            migrationBuilder.RenameIndex(
                name: "IX_Product_UserID",
                table: "Products",
                newName: "IX_Products_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_Product_CompanyID",
                table: "Products",
                newName: "IX_Products_CompanyID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Products",
                table: "Products",
                column: "ProductID");

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Products_ProductID",
                table: "Inventories",
                column: "ProductID",
                principalTable: "Products",
                principalColumn: "ProductID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_AspNetUsers_UserID",
                table: "Products",
                column: "UserID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Companies_CompanyID",
                table: "Products",
                column: "CompanyID",
                principalTable: "Companies",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
