using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddCentreNetworkFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RouteStops_RouteId",
                table: "RouteStops");

            migrationBuilder.AlterColumn<string>(
                name: "RouteNumber",
                table: "Routes",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "CentreId",
                table: "Routes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Routes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Centres",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    City = table.Column<string>(type: "text", nullable: false),
                    District = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Centres", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CentreId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bays_Centres_CentreId",
                        column: x => x.CentreId,
                        principalTable: "Centres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RouteSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    BayId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstDeparture = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    LastDeparture = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    HeadwayMinutes = table.Column<int>(type: "integer", nullable: false),
                    OperatingDays = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteSchedules_Bays_BayId",
                        column: x => x.BayId,
                        principalTable: "Bays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RouteSchedules_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RouteStops_RouteId_SequenceOrder",
                table: "RouteStops",
                columns: new[] { "RouteId", "SequenceOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Routes_CentreId_RouteNumber",
                table: "Routes",
                columns: new[] { "CentreId", "RouteNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bays_CentreId_Code",
                table: "Bays",
                columns: new[] { "CentreId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Centres_Code",
                table: "Centres",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RouteSchedules_BayId",
                table: "RouteSchedules",
                column: "BayId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteSchedules_RouteId",
                table: "RouteSchedules",
                column: "RouteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Routes_Centres_CentreId",
                table: "Routes",
                column: "CentreId",
                principalTable: "Centres",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Routes_Centres_CentreId",
                table: "Routes");

            migrationBuilder.DropTable(
                name: "RouteSchedules");

            migrationBuilder.DropTable(
                name: "Bays");

            migrationBuilder.DropTable(
                name: "Centres");

            migrationBuilder.DropIndex(
                name: "IX_RouteStops_RouteId_SequenceOrder",
                table: "RouteStops");

            migrationBuilder.DropIndex(
                name: "IX_Routes_CentreId_RouteNumber",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "CentreId",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Routes");

            migrationBuilder.AlterColumn<string>(
                name: "RouteNumber",
                table: "Routes",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.CreateIndex(
                name: "IX_RouteStops_RouteId",
                table: "RouteStops",
                column: "RouteId");
        }
    }
}
