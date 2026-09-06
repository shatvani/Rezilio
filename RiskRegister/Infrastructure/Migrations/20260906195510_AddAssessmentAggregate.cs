using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RiskRegister.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentAggregate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "assessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RiskId = table.Column<Guid>(type: "uuid", nullable: false),
                    InherentLikelihood = table.Column<int>(type: "integer", nullable: false),
                    InherentImpact = table.Column<int>(type: "integer", nullable: false),
                    InherentScore = table.Column<int>(type: "integer", nullable: false),
                    ResidualLikelihood = table.Column<int>(type: "integer", nullable: false),
                    ResidualImpact = table.Column<int>(type: "integer", nullable: false),
                    ResidualScore = table.Column<int>(type: "integer", nullable: false),
                    TargetLikelihood = table.Column<int>(type: "integer", nullable: true),
                    TargetImpact = table.Column<int>(type: "integer", nullable: true),
                    TargetScore = table.Column<int>(type: "integer", nullable: true),
                    estimated_financial_impact_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    estimated_financial_impact_currency = table.Column<string>(type: "text", nullable: true),
                    EbitdaImpactPercentage = table.Column<decimal>(type: "numeric(9,2)", nullable: true),
                    ebitda_baseline_snapshot_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    ebitda_baseline_snapshot_currency = table.Column<string>(type: "text", nullable: true),
                    ImpactContextOrgUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    ImpactContextBusinessProcessId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssessedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_assessments_risks_RiskId",
                        column: x => x.RiskId,
                        principalTable: "risks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_assessments_RiskId",
                table: "assessments",
                column: "RiskId");

            migrationBuilder.CreateIndex(
                name: "IX_assessments_TenantId",
                table: "assessments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_assessments_TenantId_ApprovalStatus",
                table: "assessments",
                columns: new[] { "TenantId", "ApprovalStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_assessments_TenantId_RiskId",
                table: "assessments",
                columns: new[] { "TenantId", "RiskId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assessments");
        }
    }
}
