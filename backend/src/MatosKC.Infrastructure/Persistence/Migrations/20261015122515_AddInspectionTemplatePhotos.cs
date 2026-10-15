using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatosKC.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInspectionTemplatePhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inspection_template_photos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    object_key = table.Column<string>(type: "text", nullable: false),
                    file_name = table.Column<string>(type: "text", nullable: false),
                    content_type = table.Column<string>(type: "text", nullable: false),
                    content_length = table.Column<long>(type: "bigint", nullable: false),
                    created_by_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inspection_template_photos", x => x.id);
                    table.ForeignKey(
                        name: "FK_inspection_template_photos_accounts_created_by_account_id",
                        column: x => x.created_by_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inspection_template_photos_created_by_account_id",
                table: "inspection_template_photos",
                column: "created_by_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_template_photos_object_key",
                table: "inspection_template_photos",
                column: "object_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inspection_template_photos");
        }
    }
}
