using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddRecoveryCopilotMemoryAndDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImpactReviewsJson",
                table: "AgentWorkflows",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NotificationDraftsJson",
                table: "AgentWorkflows",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "RecoveryMemories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CentreId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    IncidentType = table.Column<int>(type: "integer", nullable: false),
                    RouteNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: true),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: true),
                    BayId = table.Column<Guid>(type: "uuid", nullable: true),
                    DelayMinutes = table.Column<int>(type: "integer", nullable: false),
                    AvoidVehicle = table.Column<bool>(type: "boolean", nullable: false),
                    AvoidDriver = table.Column<bool>(type: "boolean", nullable: false),
                    AvoidBay = table.Column<bool>(type: "boolean", nullable: false),
                    MaxDelayMinutes = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecoveryMemories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecoveryMemories_AgentWorkflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "AgentWorkflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecoveryMemories_Centres_CentreId",
                        column: x => x.CentreId,
                        principalTable: "Centres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryMemories_CentreId_RouteNumber_IncidentType",
                table: "RecoveryMemories",
                columns: new[] { "CentreId", "RouteNumber", "IncidentType" });

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryMemories_WorkflowId",
                table: "RecoveryMemories",
                column: "WorkflowId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecoveryMemories");

            migrationBuilder.DropColumn(
                name: "ImpactReviewsJson",
                table: "AgentWorkflows");

            migrationBuilder.DropColumn(
                name: "NotificationDraftsJson",
                table: "AgentWorkflows");
        }
    }
}
