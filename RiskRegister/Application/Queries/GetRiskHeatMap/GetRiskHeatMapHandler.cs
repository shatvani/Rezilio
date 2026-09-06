namespace Rezilio.Modules.RiskRegister.Application.Queries.GetRiskHeatMap;

/// <summary>
/// A §3.1-ben ígért mátrix-lekérdezés: minden releváns Risk-hez a legutóbbi Approved
/// Assessment reziduális Likelihood/Impact/Score párja, a frontend 5×5-ös hőtérképéhez
/// (Story 1.8, §1.4). "Releváns" = van legalább egy Approved Assessment-je — egy Risk,
/// aminek még sosem volt jóváhagyott értékelése, nem jeleníthető meg a hőtérképen (nincs
/// mihez a Likelihood/Impact koordinátát rendelni).
/// </summary>
public sealed class GetRiskHeatMapHandler(RiskRegisterDbContext db, ITenantContext tenantContext)
{
    [WolverineGet("/api/riskregister/risks/heat-map")]
    [Authorize]
    public async Task<IResult> Handle([FromQuery] RiskDomain? domain, [FromQuery] RiskStatus? status, CancellationToken ct)
    {
        var riskQuery = db.Risks.AsNoTracking().Where(r => r.TenantId == tenantContext.TenantId);

        if (domain is not null)
        {
            riskQuery = riskQuery.Where(r => r.Domain == domain);
        }

        if (status is not null)
        {
            riskQuery = riskQuery.Where(r => r.Status == status);
        }

        var risks = await riskQuery.ToListAsync(ct);
        var riskIds = risks.Select(r => r.Id).ToList();

        // "Legutóbbi Approved Assessment Risk-enként" — GroupBy + rendezett First(), az
        // EF Core / Npgsql provider ezt jellemzően ROW_NUMBER()-es al-lekérdezésre fordítja.
        // Ha ez a lekérdezés futásidőben NotSupportedException-t dobna (provider-verzió
        // függő), a fallback egy explicit al-lekérdezés MAX(ApprovedAt)-tal — ld. megjegyzés
        // a PMC futtatás után, ha szükséges.
        var latestApprovedByRisk = await db.Assessments
            .AsNoTracking()
            .Where(a => a.TenantId == tenantContext.TenantId
                        && riskIds.Contains(a.RiskId)
                        && a.ApprovalStatus == ApprovalStatus.Approved)
            .GroupBy(a => a.RiskId)
            .Select(g => g.OrderByDescending(a => a.ApprovedAt).First())
            .ToDictionaryAsync(a => a.RiskId, ct);

        var items = risks
            .Where(r => latestApprovedByRisk.ContainsKey(r.Id))
            .Select(r =>
            {
                var latest = latestApprovedByRisk[r.Id];
                return new RiskHeatMapItemDto(
                    r.Id, r.Code, r.Domain, r.Title, r.Status,
                    latest.ResidualLikelihood, latest.ResidualImpact, latest.ResidualScore);
            })
            .ToList();

        return Results.Ok(items);
    }
}
