using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AllowStopSequencesPerDirection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RouteStops_RouteDirectionId",
                table: "RouteStops");

            migrationBuilder.DropIndex(
                name: "IX_RouteStops_RouteId_SequenceOrder",
                table: "RouteStops");

            migrationBuilder.CreateIndex(
                name: "IX_RouteStops_RouteDirectionId_SequenceOrder",
                table: "RouteStops",
                columns: new[] { "RouteDirectionId", "SequenceOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RouteStops_RouteId",
                table: "RouteStops",
                column: "RouteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RouteStops_RouteDirectionId_SequenceOrder",
                table: "RouteStops");

            migrationBuilder.DropIndex(
                name: "IX_RouteStops_RouteId",
                table: "RouteStops");

            migrationBuilder.CreateIndex(
                name: "IX_RouteStops_RouteDirectionId",
                table: "RouteStops",
                column: "RouteDirectionId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteStops_RouteId_SequenceOrder",
                table: "RouteStops",
                columns: new[] { "RouteId", "SequenceOrder" },
                unique: true);
        }
    }
}
