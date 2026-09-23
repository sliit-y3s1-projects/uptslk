using api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations;

/// <summary>
/// Removes development-only records that depend on the old one-way route model.
/// Terminal, fleet, driver, user, and passenger setup remains in place.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260923113000_ResetRouteOperationDemoData")]
public partial class ResetRouteOperationDemoData : Migration
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

            DELETE FROM "Trips";
            DELETE FROM "FareRules";
            DELETE FROM "RouteSchedules";
            DELETE FROM "RouteStops";
            DELETE FROM "RouteDirections";
            DELETE FROM "Routes";
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Deleted demo records cannot be reconstructed safely.
    }
}
