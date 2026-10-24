using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymCrest.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGymBillingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoEmailReceipts",
                table: "Gyms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultTaxRate",
                table: "Gyms",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "InvoicePrefix",
                table: "Gyms",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OverdueGraceDays",
                table: "Gyms",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoEmailReceipts",
                table: "Gyms");

            migrationBuilder.DropColumn(
                name: "DefaultTaxRate",
                table: "Gyms");

            migrationBuilder.DropColumn(
                name: "InvoicePrefix",
                table: "Gyms");

            migrationBuilder.DropColumn(
                name: "OverdueGraceDays",
                table: "Gyms");
        }
    }
}
