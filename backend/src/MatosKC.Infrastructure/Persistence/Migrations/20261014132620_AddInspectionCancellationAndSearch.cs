using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatosKC.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInspectionCancellationAndSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_inspections_rental_id_type",
                table: "inspections");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cancelled_at",
                table: "rentals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_reason",
                table: "inspections",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cancelled_at",
                table: "inspections",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "cancelled_by_account_id",
                table: "inspections",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_rental_clients_command_value",
                table: "rental_clients",
                column: "command_value");

            migrationBuilder.CreateIndex(
                name: "IX_inspections_cancelled_by_account_id",
                table: "inspections",
                column: "cancelled_by_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspections_rental_id_type",
                table: "inspections",
                columns: new[] { "rental_id", "type" },
                unique: true,
                filter: "status <> 'Cancelled'");

            migrationBuilder.CreateIndex(
                name: "IX_inspections_status_started_at",
                table: "inspections",
                columns: new[] { "status", "started_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_inspections_accounts_cancelled_by_account_id",
                table: "inspections",
                column: "cancelled_by_account_id",
                principalTable: "accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_inspections_accounts_cancelled_by_account_id",
                table: "inspections");

            migrationBuilder.DropIndex(
                name: "IX_rental_clients_command_value",
                table: "rental_clients");

            migrationBuilder.DropIndex(
                name: "IX_inspections_cancelled_by_account_id",
                table: "inspections");

            migrationBuilder.DropIndex(
                name: "IX_inspections_rental_id_type",
                table: "inspections");

            migrationBuilder.DropIndex(
                name: "IX_inspections_status_started_at",
                table: "inspections");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "rentals");

            migrationBuilder.DropColumn(
                name: "cancellation_reason",
                table: "inspections");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "inspections");

            migrationBuilder.DropColumn(
                name: "cancelled_by_account_id",
                table: "inspections");

            migrationBuilder.CreateIndex(
                name: "IX_inspections_rental_id_type",
                table: "inspections",
                columns: new[] { "rental_id", "type" },
                unique: true);
        }
    }
}
