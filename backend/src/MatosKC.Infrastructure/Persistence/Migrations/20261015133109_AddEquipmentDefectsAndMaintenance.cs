using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatosKC.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentDefectsAndMaintenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "existing_defect_id",
                table: "inspection_answers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "equipment_maintenances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    performed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    performed_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    work_description = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    recorded_by_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_defect_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment_maintenances", x => x.id);
                    table.ForeignKey(
                        name: "FK_equipment_maintenances_accounts_recorded_by_account_id",
                        column: x => x.recorded_by_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_maintenances_equipments_equipment_id",
                        column: x => x.equipment_id,
                        principalTable: "equipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "equipment_defects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_answer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    observation = table.Column<string>(type: "text", nullable: false),
                    reported_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    resolved_by_maintenance_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment_defects", x => x.id);
                    table.ForeignKey(
                        name: "FK_equipment_defects_accounts_resolved_by_account_id",
                        column: x => x.resolved_by_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_defects_equipment_maintenances_resolved_by_mainte~",
                        column: x => x.resolved_by_maintenance_id,
                        principalTable: "equipment_maintenances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_defects_equipments_equipment_id",
                        column: x => x.equipment_id,
                        principalTable: "equipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_defects_inspection_answers_inspection_answer_id",
                        column: x => x.inspection_answer_id,
                        principalTable: "inspection_answers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_defects_inspections_inspection_id",
                        column: x => x.inspection_id,
                        principalTable: "inspections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "equipment_defect_occurrences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    defect_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_answer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    observation = table.Column<string>(type: "text", nullable: false),
                    observed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    photo_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment_defect_occurrences", x => x.id);
                    table.ForeignKey(
                        name: "FK_equipment_defect_occurrences_equipment_defects_defect_id",
                        column: x => x.defect_id,
                        principalTable: "equipment_defects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_defect_occurrences_inspection_answers_inspection_~",
                        column: x => x.inspection_answer_id,
                        principalTable: "inspection_answers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_defect_occurrences_inspections_inspection_id",
                        column: x => x.inspection_id,
                        principalTable: "inspections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inspection_answers_existing_defect_id",
                table: "inspection_answers",
                column: "existing_defect_id");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_defect_occurrences_defect_id",
                table: "equipment_defect_occurrences",
                column: "defect_id");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_defect_occurrences_inspection_answer_id",
                table: "equipment_defect_occurrences",
                column: "inspection_answer_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_equipment_defect_occurrences_inspection_id",
                table: "equipment_defect_occurrences",
                column: "inspection_id");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_defects_equipment_id_status_reported_at",
                table: "equipment_defects",
                columns: new[] { "equipment_id", "status", "reported_at" });

            migrationBuilder.CreateIndex(
                name: "IX_equipment_defects_inspection_answer_id",
                table: "equipment_defects",
                column: "inspection_answer_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_equipment_defects_inspection_id",
                table: "equipment_defects",
                column: "inspection_id");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_defects_resolved_by_account_id",
                table: "equipment_defects",
                column: "resolved_by_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_defects_resolved_by_maintenance_id",
                table: "equipment_defects",
                column: "resolved_by_maintenance_id");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_maintenances_equipment_id_performed_at",
                table: "equipment_maintenances",
                columns: new[] { "equipment_id", "performed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_equipment_maintenances_recorded_by_account_id",
                table: "equipment_maintenances",
                column: "recorded_by_account_id");

            migrationBuilder.AddForeignKey(
                name: "FK_inspection_answers_equipment_defects_existing_defect_id",
                table: "inspection_answers",
                column: "existing_defect_id",
                principalTable: "equipment_defects",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_inspection_answers_equipment_defects_existing_defect_id",
                table: "inspection_answers");

            migrationBuilder.DropTable(
                name: "equipment_defect_occurrences");

            migrationBuilder.DropTable(
                name: "equipment_defects");

            migrationBuilder.DropTable(
                name: "equipment_maintenances");

            migrationBuilder.DropIndex(
                name: "IX_inspection_answers_existing_defect_id",
                table: "inspection_answers");

            migrationBuilder.DropColumn(
                name: "existing_defect_id",
                table: "inspection_answers");
        }
    }
}
