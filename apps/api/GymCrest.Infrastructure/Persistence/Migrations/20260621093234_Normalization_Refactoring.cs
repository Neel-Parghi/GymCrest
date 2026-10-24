using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymCrest.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Normalization_Refactoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentOnboardingStep",
                table: "UserProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsOnboarded",
                table: "UserProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "UserSecurities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsEmailVerified = table.Column<bool>(type: "boolean", nullable: false),
                    OtpCode = table.Column<string>(type: "text", nullable: true),
                    OtpExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InvitationToken = table.Column<string>(type: "text", nullable: true),
                    InvitationExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsInvitationAccepted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSecurities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSecurities_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSecurities_UserId",
                table: "UserSecurities",
                column: "UserId",
                unique: true);

            migrationBuilder.Sql(@"
                INSERT INTO ""UserSecurities"" (""Id"", ""UserId"", ""IsEmailVerified"", ""OtpCode"", ""OtpExpiry"", ""InvitationToken"", ""InvitationExpiry"", ""IsInvitationAccepted"", ""CreatedOn"", ""CreatedBy"")
                SELECT gen_random_uuid(), ""Id"", ""IsEmailVerified"", ""OtpCode"", ""OtpExpiry"", ""InvitationToken"", ""InvitationExpiry"", ""IsInvitationAccepted"", NOW(), ""Id""
                FROM ""Users"";
            ");

            migrationBuilder.Sql(@"
                UPDATE ""UserProfiles""
                SET ""CurrentOnboardingStep"" = u.""CurrentOnboardingStep"",
                    ""IsOnboarded"" = u.""IsOnboarded""
                FROM ""Users"" u
                WHERE u.""Id"" = ""UserProfiles"".""UserId"";
            ");

            migrationBuilder.DropColumn(
                name: "CurrentOnboardingStep",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "InvitationExpiry",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "InvitationToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsInvitationAccepted",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsOnboarded",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OtpCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OtpExpiry",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSecurities");

            migrationBuilder.DropColumn(
                name: "CurrentOnboardingStep",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "IsOnboarded",
                table: "UserProfiles");

            migrationBuilder.AddColumn<int>(
                name: "CurrentOnboardingStep",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "InvitationExpiry",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvitationToken",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsInvitationAccepted",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOnboarded",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OtpCode",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OtpExpiry",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
