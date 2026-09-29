using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MshMsh.Migrations
{
    /// <inheritdoc />
    public partial class AddCostPriceAndReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Products",
                newName: "SellingPrice");

            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "OrderItems",
                newName: "UnitSellingPrice");

            migrationBuilder.AddColumn<decimal>(
                name: "CostPrice",
                table: "Products",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCostPrice",
                table: "OrderItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostPrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "UnitCostPrice",
                table: "OrderItems");

            migrationBuilder.RenameColumn(
                name: "SellingPrice",
                table: "Products",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "UnitSellingPrice",
                table: "OrderItems",
                newName: "UnitPrice");
        }
    }
}
