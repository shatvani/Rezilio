namespace Rezilio.Modules.RiskRegister.Application.Commands.LinkFindingToExistingRisk;

/// <summary>
/// Egy tranzakció, csak Finding.LinkedRiskId-t írja, Risk-et nem módosít (ld. §3.1).
/// Jogosultság: RiskManager/Admin.
/// </summary>
public sealed class LinkFindingToExistingRiskHandler(RiskRegisterDbContext db, ITenantContext tenantContext, ICurrentUserContext currentUserContext)
{
    [WolverinePost("/api/riskregister/findings/{findingId}/link-risk")]
    [Authorize(Roles = "RiskManager,Admin")]
    public async Task<IResult> Handle(
        [FromRoute] Guid findingId,
        LinkFindingToExistingRiskCommand command,
        CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId;

        var finding = await db.Findings.FirstOrDefaultAsync(f => f.Id == findingId && f.TenantId == tenantId, ct);
        if (finding is null)
        {
            return Results.NotFound("Finding not found.");
        }

        var riskExists = await db.Risks.AnyAsync(r => r.Id == command.RiskId && r.TenantId == tenantId, ct);
        if (!riskExists)
        {
            return Results.NotFound("Risk not found.");
        }

        try
        {
            finding.LinkToRisk(command.RiskId);
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
