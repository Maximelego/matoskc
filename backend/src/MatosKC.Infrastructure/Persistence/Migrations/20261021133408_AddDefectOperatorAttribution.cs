using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatosKC.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDefectOperatorAttribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "operator_first_name",
                table: "equipment_defects",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "reported_by_account_id",
                table: "equipment_defects",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "operator_first_name",
                table: "equipment_defect_occurrences",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "reported_by_account_id",
                table: "equipment_defect_occurrences",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Recover each snapshot from its own source inspection, including historical aggravations.
            migrationBuilder.Sql("""
                UPDATE equipment_defects d
                SET operator_first_name = btrim(i.operator_first_name),
                    reported_by_account_id = i.created_by_account_id
                FROM inspections i WHERE i.id = d.inspection_id;

                UPDATE equipment_defect_occurrences o
                SET operator_first_name = btrim(i.operator_first_name),
                    reported_by_account_id = i.created_by_account_id
                FROM inspections i WHERE i.id = o.inspection_id;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_equipment_defects_reported_by_account_id",
                table: "equipment_defects",
                column: "reported_by_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_defect_occurrences_reported_by_account_id",
                table: "equipment_defect_occurrences",
                column: "reported_by_account_id");

            migrationBuilder.AddForeignKey(
                name: "FK_equipment_defect_occurrences_accounts_reported_by_account_id",
                table: "equipment_defect_occurrences",
                column: "reported_by_account_id",
                principalTable: "accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_equipment_defects_accounts_reported_by_account_id",
                table: "equipment_defects",
                column: "reported_by_account_id",
                principalTable: "accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_equipment_defect_occurrences_accounts_reported_by_account_id",
                table: "equipment_defect_occurrences");

            migrationBuilder.DropForeignKey(
                name: "FK_equipment_defects_accounts_reported_by_account_id",
                table: "equipment_defects");

            migrationBuilder.DropIndex(
                name: "IX_equipment_defects_reported_by_account_id",
                table: "equipment_defects");

            migrationBuilder.DropIndex(
                name: "IX_equipment_defect_occurrences_reported_by_account_id",
                table: "equipment_defect_occurrences");

            migrationBuilder.DropColumn(
                name: "operator_first_name",
                table: "equipment_defects");

            migrationBuilder.DropColumn(
                name: "reported_by_account_id",
                table: "equipment_defects");

            migrationBuilder.DropColumn(
                name: "operator_first_name",
                table: "equipment_defect_occurrences");

            migrationBuilder.DropColumn(
                name: "reported_by_account_id",
                table: "equipment_defect_occurrences");
        }
    }
}
