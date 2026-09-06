namespace Rezilio.Modules.RiskRegister.Application.Queries.GetFindingById;

public sealed class GetFindingByIdHandler(RiskRegisterDbContext db, ITenantContext tenantContext)
{
    [WolverineGet("/api/riskregister/findings/{id}")]
    [Authorize]
    public async Task<IResult> Handle([FromRoute] Guid id, CancellationToken ct)
    {
        var dto = await db.Findings
            .AsNoTracking()
            .Where(f => f.Id == id && f.TenantId == tenantContext.TenantId)
            .Select(f => new FindingDto(
                f.Id, f.TenantId, f.Source, f.Description, f.Severity, f.Status,
                f.LinkedRiskId, f.OwnerId, f.CreatedAt, f.CreatedBy, f.LastModified, f.LastModifiedBy))
            .FirstOrDefaultAsync(ct);

        if (dto is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(dto);
    }
}
