using Rezilio.Modules.RiskRegister.Application.Services;

namespace Rezilio.Modules.RiskRegister.Application.Commands.CreateRiskFromFinding;

/// <summary>
/// Egy Command-handler, egy tranzakció, két aggregátum (Risk létrehozása +
/// Finding.LinkedRiskId beállítása) — ld. §3.1 döntés (2026-09-06): az Application-
/// rétegbeli handler koordinálja mindkét írást egy SaveChanges-ben, ugyanazon
/// DbContext-en belül, mert szorosan összetartozó, azonnali, felhasználó-kezdeményezte
/// művelet, nem egy aggregátum önálló mellékhatása.
///
/// Jogosultság: RiskManager/Admin (ugyanaz, mint a TriageFinding/CreateActionFromFinding).
/// </summary>
public sealed class CreateRiskFromFindingHandler(RiskRegisterDbContext db, ITenantContext tenantContext, ICurrentUserContext currentUserContext)
{
    private const int MaxCodeGenerationAttempts = 3;

    [WolverinePost("/api/riskregister/findings/{findingId}/create-risk")]
    [Authorize(Roles = "RiskManager,Admin")]
    public async Task<IResult> Handle(
        [FromRoute] Guid findingId,
        CreateRiskFromFindingCommand command,
        CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId;

        var finding = await db.Findings.FirstOrDefaultAsync(f => f.Id == findingId && f.TenantId == tenantId, ct);
        if (finding is null)
        {
            return Results.NotFound();
        }

        var auditUser = currentUserContext.Email ?? currentUserContext.UserId ?? "unknown";

        for (int attempt = 1; attempt <= MaxCodeGenerationAttempts; attempt++)
        {
            var code = await RiskCodeGenerator.GenerateNextCodeAsync(db, tenantId, ct);

            var risk = Risk.Create(tenantId, code, command.Domain, command.Title, command.Description, command.OwnerId);
            risk.CreatedAt = DateTimeOffset.UtcNow;
            risk.CreatedBy = auditUser;

            try
            {
                finding.LinkToRisk(risk.Id);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(ex.Message);
            }

            finding.AuditChanges(auditUser);
            db.Risks.Add(risk);

            try
            {
                await db.SaveChangesAsync(ct);
                return Results.Created($"/api/riskregister/risks/{risk.Id}", new { risk.Id, risk.Code });
            }
            catch (DbUpdateException) when (attempt < MaxCodeGenerationAttempts)
            {
                db.Entry(risk).State = EntityState.Detached;
                // A finding.LinkToRisk hívást a domain-objektum állapotában nem lehet
                // "visszavonni" (immutable LinkedRiskId invariáns) — újrapróbálkozáskor
                // ezért egy friss Finding-példányt töltünk be, hogy tiszta állapotból
                // induljunk a következő körben.
                db.Entry(finding).State = EntityState.Detached;
                finding = await db.Findings.FirstAsync(f => f.Id == findingId, ct);
            }
        }

        return Results.Conflict("A Risk Code generálása nem sikerült — próbáld újra.");
    }
}
