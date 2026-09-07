using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddTripDispatchAndIncidents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Incidents_AspNetUsers_ReportedById",
                table: "Incidents");

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualDepartureAt",
                table: "Trips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BayId",
                table: "Trips",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Trips",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CentreId",
                table: "Trips",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Trips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Trips",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ReportedById",
                table: "Incidents",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "AssignedTo",
                table: "Incidents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CentreId",
                table: "Incidents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ReportedByName",
                table: "Incidents",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAt",
                table: "Incidents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Severity",
                table: "Incidents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Incidents",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_BayId",
                table: "Trips",
                column: "BayId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_CentreId",
                table: "Trips",
                column: "CentreId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_CentreId",
                table: "Incidents",
                column: "CentreId");

            migrationBuilder.AddForeignKey(
                name: "FK_Incidents_AspNetUsers_ReportedById",
                table: "Incidents",
                column: "ReportedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Incidents_Centres_CentreId",
                table: "Incidents",
                column: "CentreId",
                principalTable: "Centres",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_Bays_BayId",
                table: "Trips",
                column: "BayId",
                principalTable: "Bays",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_Centres_CentreId",
                table: "Trips",
                column: "CentreId",
                principalTable: "Centres",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Incidents_AspNetUsers_ReportedById",
                table: "Incidents");

            migrationBuilder.DropForeignKey(
                name: "FK_Incidents_Centres_CentreId",
                table: "Incidents");

            migrationBuilder.DropForeignKey(
                name: "FK_Trips_Bays_BayId",
                table: "Trips");

            migrationBuilder.DropForeignKey(
                name: "FK_Trips_Centres_CentreId",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_Trips_BayId",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_Trips_CentreId",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_CentreId",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "ActualDepartureAt",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "BayId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "CentreId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "AssignedTo",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "CentreId",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "ReportedByName",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Incidents");

            migrationBuilder.AlterColumn<Guid>(
                name: "ReportedById",
                table: "Incidents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Incidents_AspNetUsers_ReportedById",
                table: "Incidents",
                column: "ReportedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
