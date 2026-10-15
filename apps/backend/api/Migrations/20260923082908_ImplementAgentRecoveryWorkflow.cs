using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class ImplementAgentRecoveryWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "ApprovalRequests",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "AppliedAt",
                table: "ApprovalRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecisionNote",
                table: "ApprovalRequests",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Objective",
                table: "AgentWorkflows",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "CentreId",
                table: "AgentWorkflows",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "AgentWorkflows",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "AgentWorkflows",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IncidentId",
                table: "AgentWorkflows",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ProposalJson",
                table: "AgentWorkflows",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TripId",
                table: "AgentWorkflows",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "AgentName",
                table: "AgentSteps",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "DurationMs",
                table: "AgentSteps",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Error",
                table: "AgentSteps",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "AgentSteps",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkflows_CentreId_Status",
                table: "AgentWorkflows",
                columns: new[] { "CentreId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkflows_IncidentId",
                table: "AgentWorkflows",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkflows_TripId",
                table: "AgentWorkflows",
                column: "TripId");

            migrationBuilder.AddForeignKey(
                name: "FK_AgentWorkflows_Centres_CentreId",
                table: "AgentWorkflows",
                column: "CentreId",
                principalTable: "Centres",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentWorkflows_Incidents_IncidentId",
                table: "AgentWorkflows",
                column: "IncidentId",
                principalTable: "Incidents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentWorkflows_Trips_TripId",
                table: "AgentWorkflows",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgentWorkflows_Centres_CentreId",
                table: "AgentWorkflows");

            migrationBuilder.DropForeignKey(
                name: "FK_AgentWorkflows_Incidents_IncidentId",
                table: "AgentWorkflows");

            migrationBuilder.DropForeignKey(
                name: "FK_AgentWorkflows_Trips_TripId",
                table: "AgentWorkflows");

            migrationBuilder.DropIndex(
                name: "IX_AgentWorkflows_CentreId_Status",
                table: "AgentWorkflows");

            migrationBuilder.DropIndex(
                name: "IX_AgentWorkflows_IncidentId",
                table: "AgentWorkflows");

            migrationBuilder.DropIndex(
                name: "IX_AgentWorkflows_TripId",
                table: "AgentWorkflows");

            migrationBuilder.DropColumn(
                name: "AppliedAt",
                table: "ApprovalRequests");

            migrationBuilder.DropColumn(
                name: "DecisionNote",
                table: "ApprovalRequests");

            migrationBuilder.DropColumn(
                name: "CentreId",
                table: "AgentWorkflows");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "AgentWorkflows");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "AgentWorkflows");

            migrationBuilder.DropColumn(
                name: "IncidentId",
                table: "AgentWorkflows");

            migrationBuilder.DropColumn(
                name: "ProposalJson",
                table: "AgentWorkflows");

            migrationBuilder.DropColumn(
                name: "TripId",
                table: "AgentWorkflows");

            migrationBuilder.DropColumn(
                name: "DurationMs",
                table: "AgentSteps");

            migrationBuilder.DropColumn(
                name: "Error",
                table: "AgentSteps");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AgentSteps");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "ApprovalRequests",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "Objective",
                table: "AgentWorkflows",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "AgentName",
                table: "AgentSteps",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120);
        }
    }
}
