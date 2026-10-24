using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymCrest.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPartialPaymentSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "PricePaid",
                table: "MemberSubscriptions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<decimal>(
                name: "AmountPaid",
                table: "MemberSubscriptions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AmountPaid",
                table: "CustomInvoices",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            // Backfill AmountPaid for pre-existing records so previously-paid invoices
            // don't suddenly show as fully outstanding.
            migrationBuilder.Sql(@"UPDATE ""MemberSubscriptions"" SET ""AmountPaid"" = ""PricePaid"" WHERE ""PaymentStatus"" = 2;");
            migrationBuilder.Sql(@"UPDATE ""CustomInvoices"" SET ""AmountPaid"" = ""Amount"" WHERE ""Status"" = 'Paid';");

            migrationBuilder.CreateTable(
                name: "PaymentRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<int>(type: "integer", nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GymId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentRecords_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentRecords_GymMembers_MemberId",
                        column: x => x.MemberId,
                        principalTable: "GymMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentRecords_Gyms_GymId",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_BranchId",
                table: "PaymentRecords",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_GymId",
                table: "PaymentRecords",
                column: "GymId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_MemberId",
                table: "PaymentRecords",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_SourceType_SourceId",
                table: "PaymentRecords",
                columns: new[] { "SourceType", "SourceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentRecords");

            migrationBuilder.DropColumn(
                name: "AmountPaid",
                table: "MemberSubscriptions");

            migrationBuilder.DropColumn(
                name: "AmountPaid",
                table: "CustomInvoices");

            migrationBuilder.AlterColumn<decimal>(
                name: "PricePaid",
                table: "MemberSubscriptions",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);
        }
    }
}
