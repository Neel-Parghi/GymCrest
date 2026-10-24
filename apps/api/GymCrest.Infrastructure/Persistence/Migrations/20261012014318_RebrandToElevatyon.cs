using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymCrest.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RebrandToElevatyon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SaaSConfigurations",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d"),
                columns: new[] { "BillingEmail", "PlatformName" },
                values: new object[] { "admin@elevatyon.com", "Elevatyon" });

            // Accounts and memberships on the old brand's domain move to the new one (demo, demo user, super admin).
            migrationBuilder.Sql(@"UPDATE ""Users"" SET ""Email"" = regexp_replace(""Email"", '@gymforge\.com$', '@elevatyon.com', 'i') WHERE ""Email"" ILIKE '%@gymforge.com';");
            migrationBuilder.Sql(@"UPDATE ""GymMembers"" SET ""Email"" = regexp_replace(""Email"", '@gymforge\.com$', '@elevatyon.com', 'i') WHERE ""Email"" ILIKE '%@gymforge.com';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SaaSConfigurations",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d"),
                columns: new[] { "BillingEmail", "PlatformName" },
                values: new object[] { "admin@gymforge.com", "GymForge" });

            migrationBuilder.Sql(@"UPDATE ""Users"" SET ""Email"" = regexp_replace(""Email"", '@elevatyon\.com$', '@gymforge.com', 'i') WHERE ""Email"" ILIKE '%@elevatyon.com';");
            migrationBuilder.Sql(@"UPDATE ""GymMembers"" SET ""Email"" = regexp_replace(""Email"", '@elevatyon\.com$', '@gymforge.com', 'i') WHERE ""Email"" ILIKE '%@elevatyon.com';");
        }
    }
}
