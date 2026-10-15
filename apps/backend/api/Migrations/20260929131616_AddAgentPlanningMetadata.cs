using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentPlanningMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModelName",
                table: "AgentWorkflows",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelProvider",
                table: "AgentWorkflows",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OutputTokenCount",
                table: "AgentWorkflows",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PlannerInputJson",
                table: "AgentWorkflows",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "PlannerOutputJson",
                table: "AgentWorkflows",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<int>(
                name: "PlanningDurationMs",
                table: "AgentWorkflows",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PlanningFallbackReason",
                table: "AgentWorkflows",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanningMode",
                table: "AgentWorkflows",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<int>(
                name: "PromptTokenCount",
                table: "AgentWorkflows",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PromptVersion",
                table: "AgentWorkflows",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalTokenCount",
                table: "AgentWorkflows",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModelName",
                table: "AgentWorkflows");

            migrationBuilder.DropColumn(
                name: "ModelProvider",
                table: "AgentWorkflows");

            migrationBuilder.DropColumn(
                name: "OutputTokenCount",
                table: "AgentWorkflows");

            migrationBuilder.DropColumn(
                name: "PlannerInputJson",
                table: "AgentWorkflows");

            migrationBuilder.DropColumn(
                name: "PlannerOutputJson",
                table: "AgentWorkflows");

            migrationBuilder.DropColumn(
                name: "PlanningDurationMs",
                table: "AgentWorkflows");

            migrationBuilder.DropColumn(
                name: "PlanningFallbackReason",
                table: "AgentWorkflows");

            migrationBuilder.DropColumn(
                name: "PlanningMode",
                table: "AgentWorkflows");

            migrationBuilder.DropColumn(
                name: "PromptTokenCount",
                table: "AgentWorkflows");

            migrationBuilder.DropColumn(
                name: "PromptVersion",
                table: "AgentWorkflows");

            migrationBuilder.DropColumn(
                name: "TotalTokenCount",
                table: "AgentWorkflows");
        }
    }
}
