using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatosKC.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInspectionWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "equipments",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "inspection_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipment_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inspection_templates", x => x.id);
                    table.ForeignKey(
                        name: "FK_inspection_templates_equipment_categories_equipment_categor~",
                        column: x => x.equipment_category_id,
                        principalTable: "equipment_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rental_clients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    phone = table.Column<string>(type: "text", nullable: false),
                    agency_code = table.Column<int>(type: "integer", nullable: false),
                    command_value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rental_clients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inspection_template_points",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    photo_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inspection_template_points", x => x.id);
                    table.ForeignKey(
                        name: "FK_inspection_template_points_inspection_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "inspection_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rentals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: true),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    departure_inspection_id = table.Column<Guid>(type: "uuid", nullable: true),
                    return_inspection_id = table.Column<Guid>(type: "uuid", nullable: true),
                    departed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    returned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rentals", x => x.id);
                    table.ForeignKey(
                        name: "FK_rentals_agencies_agency_id",
                        column: x => x.agency_id,
                        principalTable: "agencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rentals_equipments_equipment_id",
                        column: x => x.equipment_id,
                        principalTable: "equipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rentals_rental_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "rental_clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inspections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rental_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_by_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operator_first_name = table.Column<string>(type: "text", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    validated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inspections", x => x.id);
                    table.ForeignKey(
                        name: "FK_inspections_accounts_created_by_account_id",
                        column: x => x.created_by_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inspections_inspection_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "inspection_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inspections_rentals_rental_id",
                        column: x => x.rental_id,
                        principalTable: "rentals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inspection_answers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_point_id = table.Column<Guid>(type: "uuid", nullable: false),
                    result = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    comment = table.Column<string>(type: "text", nullable: true),
                    photo_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inspection_answers", x => x.id);
                    table.ForeignKey(
                        name: "FK_inspection_answers_inspection_template_points_template_poin~",
                        column: x => x.template_point_id,
                        principalTable: "inspection_template_points",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inspection_answers_inspections_inspection_id",
                        column: x => x.inspection_id,
                        principalTable: "inspections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inspection_media",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    answer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    object_key = table.Column<string>(type: "text", nullable: false),
                    file_name = table.Column<string>(type: "text", nullable: false),
                    content_type = table.Column<string>(type: "text", nullable: false),
                    content_length = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inspection_media", x => x.id);
                    table.ForeignKey(
                        name: "FK_inspection_media_inspection_answers_answer_id",
                        column: x => x.answer_id,
                        principalTable: "inspection_answers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inspection_media_inspections_inspection_id",
                        column: x => x.inspection_id,
                        principalTable: "inspections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inspection_answers_inspection_id_template_point_id",
                table: "inspection_answers",
                columns: new[] { "inspection_id", "template_point_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inspection_answers_template_point_id",
                table: "inspection_answers",
                column: "template_point_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_media_answer_id",
                table: "inspection_media",
                column: "answer_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_media_inspection_id",
                table: "inspection_media",
                column: "inspection_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_media_object_key",
                table: "inspection_media",
                column: "object_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inspection_template_points_template_id",
                table: "inspection_template_points",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_templates_equipment_category_id_created_at",
                table: "inspection_templates",
                columns: new[] { "equipment_category_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_inspections_created_by_account_id",
                table: "inspections",
                column: "created_by_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspections_rental_id_type",
                table: "inspections",
                columns: new[] { "rental_id", "type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inspections_template_id",
                table: "inspections",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "IX_rentals_agency_id",
                table: "rentals",
                column: "agency_id");

            migrationBuilder.CreateIndex(
                name: "IX_rentals_client_id",
                table: "rentals",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ux_rentals_open_equipment",
                table: "rentals",
                column: "equipment_id",
                unique: true,
                filter: "status IN ('Draft', 'Active')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inspection_media");

            migrationBuilder.DropTable(
                name: "inspection_answers");

            migrationBuilder.DropTable(
                name: "inspection_template_points");

            migrationBuilder.DropTable(
                name: "inspections");

            migrationBuilder.DropTable(
                name: "inspection_templates");

            migrationBuilder.DropTable(
                name: "rentals");

            migrationBuilder.DropTable(
                name: "rental_clients");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "equipments");
        }
    }
}
