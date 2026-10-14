using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatosKC.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInspectionSignatureAndReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "acceptance_text",
                table: "inspections",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "report_pdf",
                table: "inspections",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "signature_png",
                table: "inspections",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "signed_at",
                table: "inspections",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "signer_name",
                table: "inspections",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "acceptance_text",
                table: "inspections");

            migrationBuilder.DropColumn(
                name: "report_pdf",
                table: "inspections");

            migrationBuilder.DropColumn(
                name: "signature_png",
                table: "inspections");

            migrationBuilder.DropColumn(
                name: "signed_at",
                table: "inspections");

            migrationBuilder.DropColumn(
                name: "signer_name",
                table: "inspections");
        }
    }
}
