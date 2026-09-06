namespace Rezilio.Modules.RiskRegister.Application.Commands.CloseRisk;

/// <summary>Jogosultság: RiskManager (ld. §3.1). Csak Active/Treated állapotból hívható.</summary>
public sealed class CloseRiskHandler(RiskRegisterDbContext db, ITenantContext tenantContext, ICurrentUserContext currentUserContext)
{
    [WolverinePost("/api/riskregister/risks/{id}/close")]
    [Authorize(Roles = "RiskManager")]
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
            risk.Close();
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
