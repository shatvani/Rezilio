namespace Rezilio.Modules.RiskRegister.Application.Commands.CreateFinding;

/// <summary>
/// Jogosultság: bárki, akinek van RiskRegister modul-hozzáférése — nincs szűkebb
/// szerepkör-megkötés (ld. §3.2, a bemenet gyakran külső eredetű: auditor, incidens,
/// önértékelés).
/// </summary>
public sealed class CreateFindingHandler(RiskRegisterDbContext db, ITenantContext tenantContext, ICurrentUserContext currentUserContext)
{
    [WolverinePost("/api/riskregister/findings")]
    [Authorize]
    public async Task<IResult> Handle(CreateFindingCommand command, CancellationToken ct)
    {
        var finding = Finding.Create(tenantContext.TenantId, command.Source, command.Description, command.Severity);
        finding.CreatedAt = DateTimeOffset.UtcNow;
        finding.CreatedBy = currentUserContext.Email ?? currentUserContext.UserId ?? "unknown";

        db.Findings.Add(finding);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/riskregister/findings/{finding.Id}", new { finding.Id });
    }
}
