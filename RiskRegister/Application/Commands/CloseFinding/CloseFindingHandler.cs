namespace Rezilio.Modules.RiskRegister.Application.Commands.CloseFinding;

/// <summary>Jogosultság: RiskManager/Admin (ld. §3.2). Csak Linked/ActionCreated állapotból hívható.</summary>
public sealed class CloseFindingHandler(RiskRegisterDbContext db, ITenantContext tenantContext, ICurrentUserContext currentUserContext)
{
    [WolverinePost("/api/riskregister/findings/{id}/close")]
    [Authorize(Roles = "RiskManager,Admin")]
    public async Task<IResult> Handle([FromRoute] Guid id, CancellationToken ct)
    {
        var finding = await db.Findings
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantContext.TenantId, ct);

        if (finding is null)
        {
            return Results.NotFound();
        }

        try
        {
            finding.Close();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ex.Message);
        }

        finding.AuditChanges(currentUserContext.Email ?? currentUserContext.UserId ?? "unknown");
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
