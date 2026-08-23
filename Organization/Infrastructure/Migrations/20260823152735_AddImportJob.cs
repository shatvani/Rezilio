using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Organization.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImportJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "import_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TotalRows = table.Column<int>(type: "integer", nullable: false),
                    SuccessRows = table.Column<int>(type: "integer", nullable: false),
                    ErrorRows = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    results = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_import_jobs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_import_jobs_TenantId",
                table: "import_jobs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_import_jobs_TenantId_EntityType",
                table: "import_jobs",
                columns: new[] { "TenantId", "EntityType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "import_jobs");
        }
    }
}
