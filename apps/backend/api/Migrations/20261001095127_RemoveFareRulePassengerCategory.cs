using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFareRulePassengerCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Only the Adult (0) fare is kept as the single standard fare per route;
            // Student/Senior/Child rows would collide on the new unique RouteId index.
            // Bookings store their own Fare and are not linked to FareRules.
            migrationBuilder.Sql(@"DELETE FROM ""FareRules"" WHERE ""PassengerCategory"" <> 0;");

            migrationBuilder.DropIndex(
                name: "IX_FareRules_RouteId_PassengerCategory",
                table: "FareRules");

            migrationBuilder.DropColumn(
                name: "PassengerCategory",
                table: "FareRules");

            migrationBuilder.CreateIndex(
                name: "IX_FareRules_RouteId",
                table: "FareRules",
                column: "RouteId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FareRules_RouteId",
                table: "FareRules");

            migrationBuilder.AddColumn<int>(
                name: "PassengerCategory",
                table: "FareRules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_FareRules_RouteId_PassengerCategory",
                table: "FareRules",
                columns: new[] { "RouteId", "PassengerCategory" },
                unique: true);
        }
    }
}
