using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Organization.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationEbitdaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "default_annual_ebitda_amount",
                table: "tenant_settings",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "default_annual_ebitda_currency",
                table: "tenant_settings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "annual_ebitda_amount",
                table: "organizational_units",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "annual_ebitda_currency",
                table: "organizational_units",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "annual_ebitda_amount",
                table: "BusinessProcesses",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "annual_ebitda_currency",
                table: "BusinessProcesses",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "default_annual_ebitda_amount",
                table: "tenant_settings");

            migrationBuilder.DropColumn(
                name: "default_annual_ebitda_currency",
                table: "tenant_settings");

            migrationBuilder.DropColumn(
                name: "annual_ebitda_amount",
                table: "organizational_units");

            migrationBuilder.DropColumn(
                name: "annual_ebitda_currency",
                table: "organizational_units");

            migrationBuilder.DropColumn(
                name: "annual_ebitda_amount",
                table: "BusinessProcesses");

            migrationBuilder.DropColumn(
                name: "annual_ebitda_currency",
                table: "BusinessProcesses");
        }
    }
}
