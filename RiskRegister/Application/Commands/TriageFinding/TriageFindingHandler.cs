namespace Rezilio.Modules.RiskRegister.Application.Commands.TriageFinding;

/// <summary>Jogosultság: RiskManager/Admin (ld. §3.2, §2.2 triázs-definíció).</summary>
public sealed class TriageFindingHandler(RiskRegisterDbContext db, ITenantContext tenantContext, ICurrentUserContext currentUserContext)
{
    [WolverinePost("/api/riskregister/findings/{id}/triage")]
    [Authorize(Roles = "RiskManager,Admin")]
    public async Task<IResult> Handle([FromRoute] Guid id, TriageFindingCommand command, CancellationToken ct)
    {
        var finding = await db.Findings
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantContext.TenantId, ct);

        if (finding is null)
        {
            return Results.NotFound();
        }

        try
        {
            finding.Triage(command.OwnerId);
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
