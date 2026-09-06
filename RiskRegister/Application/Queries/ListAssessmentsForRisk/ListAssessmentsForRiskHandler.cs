namespace Rezilio.Modules.RiskRegister.Application.Queries.ListAssessmentsForRisk;

/// <summary>
/// Egy adott Risk teljes értékelési előzménye — a történeti (Approved/Rejected) rekordok is
/// szerepelnek, nem csak az aktuális aktív Assessment (ld. docs/design/risk-register-design.md
/// §3.3 Query-lista).
/// </summary>
public sealed class ListAssessmentsForRiskHandler(RiskRegisterDbContext db, ITenantContext tenantContext)
{
    [WolverineGet("/api/riskregister/risks/{riskId}/assessments")]
    [Authorize]
    public async Task<IResult> Handle([FromRoute] Guid riskId, CancellationToken ct)
    {
        var assessments = await db.Assessments
            .AsNoTracking()
            .Where(a => a.RiskId == riskId && a.TenantId == tenantContext.TenantId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        return Results.Ok(assessments.Select(AssessmentDto.FromEntity).ToList());
    }
}
