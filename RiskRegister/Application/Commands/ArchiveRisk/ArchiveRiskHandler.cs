namespace Rezilio.Modules.RiskRegister.Application.Commands.ArchiveRisk;

/// <summary>Jogosultság: RiskManager/Admin (ld. §3.1). Csak Closed állapotból hívható.</summary>
public sealed class ArchiveRiskHandler(RiskRegisterDbContext db, ITenantContext tenantContext, ICurrentUserContext currentUserContext)
{
    [WolverinePost("/api/riskregister/risks/{id}/archive")]
    [Authorize(Roles = "RiskManager,Admin")]
    public async Task<IResult> Handle([FromRoute] Guid id, CancellationToken ct)
    {
        var risk = await db.Risks
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantContext.TenantId, ct);

        if (risk is null)
        {
            return Results.NotFound();
        }

        try
        {
            risk.Archive();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ex.Message);
        }

        risk.AuditChanges(currentUserContext.Email ?? currentUserContext.UserId ?? "unknown");
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
