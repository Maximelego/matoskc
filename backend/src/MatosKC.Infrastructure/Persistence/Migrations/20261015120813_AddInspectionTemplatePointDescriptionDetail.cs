using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatosKC.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInspectionTemplatePointDescriptionDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description_detail",
                table: "inspection_template_points",
                type: "text",
                nullable: false,
                defaultValue: "Le point de contrôle n'a pas de description détaillée.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "description_detail",
                table: "inspection_template_points");
        }
    }
}
