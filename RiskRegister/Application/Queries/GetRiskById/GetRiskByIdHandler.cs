namespace Rezilio.Modules.RiskRegister.Application.Queries.GetRiskById;

/// <summary>
/// Részletnézet. A §3.1 doc-ban leírt "más aggregátumokra is átnyúló" bővített DTO
/// (legutóbbi Approved Assessment pontszáma, aktív TreatmentPlan állapota) csak azután
/// bővíthető, hogy az Assessment/TreatmentPlan aggregátumok is elkészültek (Phase 2/3) —
/// egyelőre a Risk saját mezőit adja vissza.
/// </summary>
public sealed class GetRiskByIdHandler(RiskRegisterDbContext db, ITenantContext tenantContext)
{
    [WolverineGet("/api/riskregister/risks/{id}")]
    [Authorize]
    public async Task<IResult> Handle([FromRoute] Guid id, CancellationToken ct)
    {
        var dto = await db.Risks
            .AsNoTracking()
            .Where(r => r.Id == id && r.TenantId == tenantContext.TenantId)
            .Select(r => new RiskDto(
                r.Id, r.TenantId, r.Code, r.Domain, r.Title, r.Description,
                r.OwnerId, r.Status, r.CopiedFromRiskId,
                r.CreatedAt, r.CreatedBy, r.LastModified, r.LastModifiedBy))
            .FirstOrDefaultAsync(ct);

        if (dto is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(dto);
    }
}
