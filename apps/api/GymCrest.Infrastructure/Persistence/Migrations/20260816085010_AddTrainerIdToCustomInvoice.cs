using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymCrest.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainerIdToCustomInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TrainerId",
                table: "CustomInvoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomInvoices_TrainerId",
                table: "CustomInvoices",
                column: "TrainerId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomInvoices_Staff_TrainerId",
                table: "CustomInvoices",
                column: "TrainerId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomInvoices_Staff_TrainerId",
                table: "CustomInvoices");

            migrationBuilder.DropIndex(
                name: "IX_CustomInvoices_TrainerId",
                table: "CustomInvoices");

            migrationBuilder.DropColumn(
                name: "TrainerId",
                table: "CustomInvoices");
        }
    }
}
