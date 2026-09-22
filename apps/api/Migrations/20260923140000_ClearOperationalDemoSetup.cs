using api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations;

/// <summary>
/// Clears development operational records while intentionally retaining identity users,
/// passengers, and their wallets.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260923140000_ClearOperationalDemoSetup")]
public partial class ClearOperationalDemoSetup : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM "ApprovalRequests"
            WHERE "WorkflowId" IN (SELECT "Id" FROM "AgentWorkflows" WHERE "BookingId" IS NOT NULL);

            DELETE FROM "AgentSteps"
            WHERE "WorkflowId" IN (SELECT "Id" FROM "AgentWorkflows" WHERE "BookingId" IS NOT NULL);

            DELETE FROM "AgentWorkflows" WHERE "BookingId" IS NOT NULL;
            DELETE FROM "PaymentRefunds";
            DELETE FROM "Payments";
            DELETE FROM "PaymentWebhookEvents";
            DELETE FROM "Transactions" WHERE "BookingId" IS NOT NULL;
            DELETE FROM "Bookings";

            DELETE FROM "SupportRequests";
            DELETE FROM "Incidents";
            DELETE FROM "Trips";
            DELETE FROM "FareRules";
            DELETE FROM "RouteSchedules";
            DELETE FROM "RouteStops";
            DELETE FROM "RouteDirections";
            DELETE FROM "Routes";

            DELETE FROM "MaintenanceRecords";
            DELETE FROM "Drivers";
            DELETE FROM "Vehicles";
            DELETE FROM "Bays";
            DELETE FROM "Centres";
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Deleted development records cannot be reconstructed safely.
    }
}
