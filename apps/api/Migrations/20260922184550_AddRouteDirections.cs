using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteDirections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RouteDirectionId",
                table: "Trips",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RouteDirectionId",
                table: "RouteStops",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RouteDirectionId",
                table: "RouteSchedules",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RouteDirections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartCentreId = table.Column<Guid>(type: "uuid", nullable: false),
                    EndCentreId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DistanceKm = table.Column<decimal>(type: "numeric", nullable: false),
                    EstimatedDurationMin = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteDirections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteDirections_Centres_EndCentreId",
                        column: x => x.EndCentreId,
                        principalTable: "Centres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RouteDirections_Centres_StartCentreId",
                        column: x => x.StartCentreId,
                        principalTable: "Centres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RouteDirections_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trips_RouteDirectionId",
                table: "Trips",
                column: "RouteDirectionId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteStops_RouteDirectionId",
                table: "RouteStops",
                column: "RouteDirectionId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteSchedules_RouteDirectionId",
                table: "RouteSchedules",
                column: "RouteDirectionId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteDirections_EndCentreId",
                table: "RouteDirections",
                column: "EndCentreId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteDirections_RouteId_StartCentreId_EndCentreId",
                table: "RouteDirections",
                columns: new[] { "RouteId", "StartCentreId", "EndCentreId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RouteDirections_StartCentreId",
                table: "RouteDirections",
                column: "StartCentreId");

            migrationBuilder.AddForeignKey(
                name: "FK_RouteSchedules_RouteDirections_RouteDirectionId",
                table: "RouteSchedules",
                column: "RouteDirectionId",
                principalTable: "RouteDirections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RouteStops_RouteDirections_RouteDirectionId",
                table: "RouteStops",
                column: "RouteDirectionId",
                principalTable: "RouteDirections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_RouteDirections_RouteDirectionId",
                table: "Trips",
                column: "RouteDirectionId",
                principalTable: "RouteDirections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RouteSchedules_RouteDirections_RouteDirectionId",
                table: "RouteSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_RouteStops_RouteDirections_RouteDirectionId",
                table: "RouteStops");

            migrationBuilder.DropForeignKey(
                name: "FK_Trips_RouteDirections_RouteDirectionId",
                table: "Trips");

            migrationBuilder.DropTable(
                name: "RouteDirections");

            migrationBuilder.DropIndex(
                name: "IX_Trips_RouteDirectionId",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_RouteStops_RouteDirectionId",
                table: "RouteStops");

            migrationBuilder.DropIndex(
                name: "IX_RouteSchedules_RouteDirectionId",
                table: "RouteSchedules");

            migrationBuilder.DropColumn(
                name: "RouteDirectionId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "RouteDirectionId",
                table: "RouteStops");

            migrationBuilder.DropColumn(
                name: "RouteDirectionId",
                table: "RouteSchedules");
        }
    }
}
