using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatosKC.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IndependentInspectionDefects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "answer_id",
                table: "inspection_media",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "defect_review_id",
                table: "inspection_media",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "finding_id",
                table: "inspection_media",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "inspection_answer_id",
                table: "equipment_defects",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");


            migrationBuilder.AddColumn<Guid>(
                name: "inspection_finding_id",
                table: "equipment_defects",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "inspection_answer_id",
                table: "equipment_defect_occurrences",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "inspection_defect_review_id",
                table: "equipment_defect_occurrences",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "inspection_finding_id",
                table: "equipment_defect_occurrences",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "inspection_defect_reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    defect_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    historical_observation = table.Column<string>(type: "text", nullable: false),
                    observed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    result = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    observation = table.Column<string>(type: "text", nullable: true),
                    historical_photo_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    photo_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inspection_defect_reviews", x => x.id);
                    table.ForeignKey(
                        name: "FK_inspection_defect_reviews_equipment_defects_defect_id",
                        column: x => x.defect_id,
                        principalTable: "equipment_defects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inspection_defect_reviews_inspections_inspection_id",
                        column: x => x.inspection_id,
                        principalTable: "inspections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inspection_findings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_point_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_removed = table.Column<bool>(type: "boolean", nullable: false),
                    observation = table.Column<string>(type: "text", nullable: false),
                    photo_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inspection_findings", x => x.id);
                    table.ForeignKey(
                        name: "FK_inspection_findings_inspection_template_points_template_poi~",
                        column: x => x.template_point_id,
                        principalTable: "inspection_template_points",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inspection_findings_inspections_inspection_id",
                        column: x => x.inspection_id,
                        principalTable: "inspections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inspection_media_defect_review_id",
                table: "inspection_media",
                column: "defect_review_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_media_finding_id",
                table: "inspection_media",
                column: "finding_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_media_source",
                table: "inspection_media",
                sql: "num_nonnulls(answer_id, finding_id, defect_review_id) = 1");


            migrationBuilder.CreateIndex(
                name: "IX_equipment_defects_inspection_finding_id",
                table: "equipment_defects",
                column: "inspection_finding_id",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_defect_source",
                table: "equipment_defects",
                sql: "num_nonnulls(inspection_answer_id, inspection_finding_id) = 1");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_defect_occurrences_inspection_defect_review_id",
                table: "equipment_defect_occurrences",
                column: "inspection_defect_review_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_equipment_defect_occurrences_inspection_finding_id",
                table: "equipment_defect_occurrences",
                column: "inspection_finding_id",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_occurrence_source",
                table: "equipment_defect_occurrences",
                sql: "num_nonnulls(inspection_answer_id, inspection_finding_id, inspection_defect_review_id) = 1");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_defect_reviews_defect_id",
                table: "inspection_defect_reviews",
                column: "defect_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_defect_reviews_inspection_id_defect_id",
                table: "inspection_defect_reviews",
                columns: new[] { "inspection_id", "defect_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inspection_findings_inspection_id",
                table: "inspection_findings",
                column: "inspection_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_findings_template_point_id",
                table: "inspection_findings",
                column: "template_point_id");

            migrationBuilder.AddForeignKey(
                name: "FK_equipment_defect_occurrences_inspection_defect_reviews_insp~",
                table: "equipment_defect_occurrences",
                column: "inspection_defect_review_id",
                principalTable: "inspection_defect_reviews",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_equipment_defect_occurrences_inspection_findings_inspection~",
                table: "equipment_defect_occurrences",
                column: "inspection_finding_id",
                principalTable: "inspection_findings",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);


            migrationBuilder.AddForeignKey(
                name: "FK_equipment_defects_inspection_findings_inspection_finding_id",
                table: "equipment_defects",
                column: "inspection_finding_id",
                principalTable: "inspection_findings",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_inspection_media_inspection_defect_reviews_defect_review_id",
                table: "inspection_media",
                column: "defect_review_id",
                principalTable: "inspection_defect_reviews",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_inspection_media_inspection_findings_finding_id",
                table: "inspection_media",
                column: "finding_id",
                principalTable: "inspection_findings",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
            // Preserve open drafts while leaving validated inspections and their stored PDFs untouched.
            migrationBuilder.Sql("""
                INSERT INTO inspection_defect_reviews
                    (id, inspection_id, defect_id, description, historical_observation, observed_at,
                     result, observation, historical_photo_ids, photo_ids)
                SELECT gen_random_uuid(), i.id, d.id, COALESCE(o.description, d.description),
                       COALESCE(o.observation, d.observation), COALESCE(o.observed_at, d.reported_at),
                       NULL, NULL, COALESCE(o.photo_ids, ARRAY[]::uuid[]), ARRAY[]::uuid[]
                FROM inspections i
                JOIN rentals r ON r.id = i.rental_id
                JOIN equipment_defects d ON d.equipment_id = r.equipment_id
                LEFT JOIN LATERAL (
                    SELECT description, observation, observed_at, photo_ids
                    FROM equipment_defect_occurrences WHERE defect_id = d.id
                    ORDER BY observed_at DESC, id DESC LIMIT 1
                ) o ON TRUE
                WHERE i.status = 'Draft' AND (d.status = 'Open' OR EXISTS (
                    SELECT 1 FROM inspection_answers a WHERE a.inspection_id = i.id AND a.existing_defect_id = d.id
                ));

                -- Preserve linked draft evidence without assuming an aggravation.
                -- The operator must explicitly choose the new review result.
                UPDATE inspection_defect_reviews v
                SET result = NULL, observation = a.comment, photo_ids = a.photo_ids
                FROM inspection_answers a
                WHERE a.inspection_id = v.inspection_id AND a.existing_defect_id = v.defect_id
                  AND a.id = (SELECT a2.id FROM inspection_answers a2
                              WHERE a2.inspection_id = v.inspection_id AND a2.existing_defect_id = v.defect_id
                              ORDER BY a2.id LIMIT 1);

                UPDATE inspection_media m
                SET answer_id = NULL, defect_review_id = v.id
                FROM inspection_answers a, inspection_defect_reviews v
                WHERE m.answer_id = a.id AND a.inspection_id = v.inspection_id
                  AND a.existing_defect_id = v.defect_id;

                -- Preserve every linked answer's photos and observations if several answers
                -- referenced the same defect in the old draft.
                UPDATE inspection_defect_reviews v
                SET photo_ids = COALESCE((SELECT array_agg(DISTINCT photo) FROM inspection_answers a,
                                    unnest(a.photo_ids) photo
                                    WHERE a.inspection_id = v.inspection_id AND a.existing_defect_id = v.defect_id), ARRAY[]::uuid[]),
                    observation = (SELECT string_agg(a.comment, E'\n' ORDER BY a.id) FROM inspection_answers a
                                    WHERE a.inspection_id = v.inspection_id AND a.existing_defect_id = v.defect_id)
                WHERE EXISTS (SELECT 1 FROM inspection_answers a
                              WHERE a.inspection_id = v.inspection_id AND a.existing_defect_id = v.defect_id);

                UPDATE inspection_answers a
                SET result = 'Compliant', comment = NULL, existing_defect_id = NULL, photo_ids = ARRAY[]::uuid[]
                WHERE EXISTS (SELECT 1 FROM inspection_defect_reviews v
                              WHERE v.inspection_id = a.inspection_id AND v.defect_id = a.existing_defect_id);

                UPDATE inspections i
                SET signer_name = NULL, signed_at = NULL, acceptance_text = NULL, signature_png = NULL
                WHERE i.status = 'Draft' AND EXISTS (
                    SELECT 1 FROM inspection_defect_reviews v WHERE v.inspection_id = i.id
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM inspection_defect_reviews)
                       OR EXISTS (SELECT 1 FROM inspection_findings) THEN
                        RAISE EXCEPTION 'Cannot roll back while inspection defect snapshots or findings exist.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_equipment_defect_occurrences_inspection_defect_reviews_insp~",
                table: "equipment_defect_occurrences");

            migrationBuilder.DropForeignKey(
                name: "FK_equipment_defect_occurrences_inspection_findings_inspection~",
                table: "equipment_defect_occurrences");


            migrationBuilder.DropForeignKey(
                name: "FK_equipment_defects_inspection_findings_inspection_finding_id",
                table: "equipment_defects");

            migrationBuilder.DropForeignKey(
                name: "FK_inspection_media_inspection_defect_reviews_defect_review_id",
                table: "inspection_media");

            migrationBuilder.DropForeignKey(
                name: "FK_inspection_media_inspection_findings_finding_id",
                table: "inspection_media");

            migrationBuilder.DropTable(
                name: "inspection_defect_reviews");

            migrationBuilder.DropTable(
                name: "inspection_findings");

            migrationBuilder.DropIndex(
                name: "IX_inspection_media_defect_review_id",
                table: "inspection_media");

            migrationBuilder.DropIndex(
                name: "IX_inspection_media_finding_id",
                table: "inspection_media");

            migrationBuilder.DropCheckConstraint(
                name: "ck_media_source",
                table: "inspection_media");


            migrationBuilder.DropIndex(
                name: "IX_equipment_defects_inspection_finding_id",
                table: "equipment_defects");

            migrationBuilder.DropCheckConstraint(
                name: "ck_defect_source",
                table: "equipment_defects");

            migrationBuilder.DropIndex(
                name: "IX_equipment_defect_occurrences_inspection_defect_review_id",
                table: "equipment_defect_occurrences");

            migrationBuilder.DropIndex(
                name: "IX_equipment_defect_occurrences_inspection_finding_id",
                table: "equipment_defect_occurrences");

            migrationBuilder.DropCheckConstraint(
                name: "ck_occurrence_source",
                table: "equipment_defect_occurrences");

            migrationBuilder.DropColumn(
                name: "defect_review_id",
                table: "inspection_media");

            migrationBuilder.DropColumn(
                name: "finding_id",
                table: "inspection_media");


            migrationBuilder.DropColumn(
                name: "inspection_finding_id",
                table: "equipment_defects");

            migrationBuilder.DropColumn(
                name: "inspection_defect_review_id",
                table: "equipment_defect_occurrences");

            migrationBuilder.DropColumn(
                name: "inspection_finding_id",
                table: "equipment_defect_occurrences");

            migrationBuilder.AlterColumn<Guid>(
                name: "answer_id",
                table: "inspection_media",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "inspection_answer_id",
                table: "equipment_defects",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "inspection_answer_id",
                table: "equipment_defect_occurrences",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
