using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatosKC.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ScopeResourcesByAgency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing resources and their rental/inspection histories are assigned to agency 83.
            migrationBuilder.Sql("""
                INSERT INTO agencies (id, name, code)
                SELECT '83000000-0000-0000-0000-000000000083'::uuid, '83 - Epinal', 83
                WHERE NOT EXISTS (SELECT 1 FROM agencies WHERE code = 83);
                UPDATE rentals SET agency_id = (SELECT id FROM agencies WHERE code = 83);
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_equipments_equipment_categories_category_id",
                table: "equipments");

            migrationBuilder.DropForeignKey(
                name: "FK_inspection_templates_equipment_categories_equipment_categor~",
                table: "inspection_templates");

            migrationBuilder.DropForeignKey(
                name: "FK_rentals_equipments_equipment_id",
                table: "rentals");

            migrationBuilder.DropIndex(
                name: "IX_rentals_agency_id",
                table: "rentals");

            migrationBuilder.DropIndex(
                name: "ux_equipment_categories_name",
                table: "equipment_categories");

            migrationBuilder.AlterColumn<Guid>(
                name: "agency_id",
                table: "rentals",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "agency_id",
                table: "inspection_templates",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "agency_id",
                table: "inspection_template_photos",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "agency_id",
                table: "equipments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "agency_id",
                table: "equipment_categories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("""
                UPDATE equipment_categories SET agency_id = (SELECT id FROM agencies WHERE code = 83);
                UPDATE equipments SET agency_id = (SELECT id FROM agencies WHERE code = 83);
                UPDATE inspection_templates SET agency_id = (SELECT id FROM agencies WHERE code = 83);
                UPDATE inspection_template_photos SET agency_id = (SELECT id FROM agencies WHERE code = 83);
                """);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_equipments_agency_id_id",
                table: "equipments",
                columns: new[] { "agency_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_equipment_categories_agency_id_id",
                table: "equipment_categories",
                columns: new[] { "agency_id", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_rentals_agency_id_equipment_id",
                table: "rentals",
                columns: new[] { "agency_id", "equipment_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inspection_templates_agency_id_equipment_category_id",
                table: "inspection_templates",
                columns: new[] { "agency_id", "equipment_category_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inspection_template_photos_agency_id",
                table: "inspection_template_photos",
                column: "agency_id");

            migrationBuilder.CreateIndex(
                name: "IX_equipments_agency_id_category_id",
                table: "equipments",
                columns: new[] { "agency_id", "category_id" });

            migrationBuilder.CreateIndex(
                name: "ux_equipment_categories_name",
                table: "equipment_categories",
                columns: new[] { "agency_id", "name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_equipment_categories_agencies_agency_id",
                table: "equipment_categories",
                column: "agency_id",
                principalTable: "agencies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_equipments_agencies_agency_id",
                table: "equipments",
                column: "agency_id",
                principalTable: "agencies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_equipments_equipment_categories_agency_id_category_id",
                table: "equipments",
                columns: new[] { "agency_id", "category_id" },
                principalTable: "equipment_categories",
                principalColumns: new[] { "agency_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_inspection_template_photos_agencies_agency_id",
                table: "inspection_template_photos",
                column: "agency_id",
                principalTable: "agencies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_inspection_templates_agencies_agency_id",
                table: "inspection_templates",
                column: "agency_id",
                principalTable: "agencies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_inspection_templates_equipment_categories_agency_id_equipme~",
                table: "inspection_templates",
                columns: new[] { "agency_id", "equipment_category_id" },
                principalTable: "equipment_categories",
                principalColumns: new[] { "agency_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_rentals_equipments_agency_id_equipment_id",
                table: "rentals",
                columns: new[] { "agency_id", "equipment_id" },
                principalTable: "equipments",
                principalColumns: new[] { "agency_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM equipment_categories WHERE agency_id <> (SELECT id FROM agencies WHERE code = 83)) THEN
                        RAISE EXCEPTION 'Cannot remove agency ownership while multiple agencies own resources.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_equipment_categories_agencies_agency_id",
                table: "equipment_categories");

            migrationBuilder.DropForeignKey(
                name: "FK_equipments_agencies_agency_id",
                table: "equipments");

            migrationBuilder.DropForeignKey(
                name: "FK_equipments_equipment_categories_agency_id_category_id",
                table: "equipments");

            migrationBuilder.DropForeignKey(
                name: "FK_inspection_template_photos_agencies_agency_id",
                table: "inspection_template_photos");

            migrationBuilder.DropForeignKey(
                name: "FK_inspection_templates_agencies_agency_id",
                table: "inspection_templates");

            migrationBuilder.DropForeignKey(
                name: "FK_inspection_templates_equipment_categories_agency_id_equipme~",
                table: "inspection_templates");

            migrationBuilder.DropForeignKey(
                name: "FK_rentals_equipments_agency_id_equipment_id",
                table: "rentals");

            migrationBuilder.DropIndex(
                name: "IX_rentals_agency_id_equipment_id",
                table: "rentals");

            migrationBuilder.DropIndex(
                name: "IX_inspection_templates_agency_id_equipment_category_id",
                table: "inspection_templates");

            migrationBuilder.DropIndex(
                name: "IX_inspection_template_photos_agency_id",
                table: "inspection_template_photos");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_equipments_agency_id_id",
                table: "equipments");

            migrationBuilder.DropIndex(
                name: "IX_equipments_agency_id_category_id",
                table: "equipments");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_equipment_categories_agency_id_id",
                table: "equipment_categories");

            migrationBuilder.DropIndex(
                name: "ux_equipment_categories_name",
                table: "equipment_categories");

            migrationBuilder.DropColumn(
                name: "agency_id",
                table: "inspection_templates");

            migrationBuilder.DropColumn(
                name: "agency_id",
                table: "inspection_template_photos");

            migrationBuilder.DropColumn(
                name: "agency_id",
                table: "equipments");

            migrationBuilder.DropColumn(
                name: "agency_id",
                table: "equipment_categories");

            migrationBuilder.AlterColumn<Guid>(
                name: "agency_id",
                table: "rentals",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_rentals_agency_id",
                table: "rentals",
                column: "agency_id");

            migrationBuilder.CreateIndex(
                name: "ux_equipment_categories_name",
                table: "equipment_categories",
                column: "name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_equipments_equipment_categories_category_id",
                table: "equipments",
                column: "category_id",
                principalTable: "equipment_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_inspection_templates_equipment_categories_equipment_categor~",
                table: "inspection_templates",
                column: "equipment_category_id",
                principalTable: "equipment_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_rentals_equipments_equipment_id",
                table: "rentals",
                column: "equipment_id",
                principalTable: "equipments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
