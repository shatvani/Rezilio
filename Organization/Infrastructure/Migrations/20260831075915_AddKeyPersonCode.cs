using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Organization.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddKeyPersonCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "KeyPersons",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_KeyPersons_TenantId_Code",
                table: "KeyPersons",
                columns: new[] { "TenantId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KeyPersons_TenantId_Code",
                table: "KeyPersons");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "KeyPersons");
        }
    }
}
