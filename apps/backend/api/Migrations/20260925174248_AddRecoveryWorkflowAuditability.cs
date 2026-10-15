using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddRecoveryWorkflowAuditability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlanJson",
                table: "AgentWorkflows",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "ValidationJson",
                table: "AgentWorkflows",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "AgentSteps",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ToolCallsJson",
                table: "AgentSteps",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlanJson",
                table: "AgentWorkflows");

            migrationBuilder.DropColumn(
                name: "ValidationJson",
                table: "AgentWorkflows");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "AgentSteps");

            migrationBuilder.DropColumn(
                name: "ToolCallsJson",
                table: "AgentSteps");
        }
    }
}
