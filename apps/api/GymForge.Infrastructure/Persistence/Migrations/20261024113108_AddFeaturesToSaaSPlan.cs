using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFeaturesToSaaSPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "Features",
                table: "Plans",
                type: "text[]",
                nullable: true);

            // Existing plans start with the list the landing page used to hard-code; edit per plan in Super Admin.
            migrationBuilder.Sql(@"
                UPDATE ""Plans"" SET ""Features"" = ARRAY[
                    'Member records, plans & one-click renewals',
                    'Invoices & online payments via Razorpay',
                    'Desk check-in & daily attendance',
                    'Staff & trainer accounts',
                    'Workout & diet plans members follow in their free app',
                    'Automatic renewal reminders & announcements',
                    'Inventory & shop sales'
                ] WHERE ""Features"" IS NULL AND ""IsTrial"" = false;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Features",
                table: "Plans");
        }
    }
}
