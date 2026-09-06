using Rezilio.Modules.RiskRegister.Application.Services;
using Wolverine;

namespace Rezilio.Modules.RiskRegister.Application.Commands.ReopenAssessment;

/// <summary>
/// Jogosultság: RiskOwner. Rejected → Draft (átdolgozás megkezdése) — külön, dedikált
/// Command (2026-09-06-i döntés, ld. §2.2/§3.3), elkülönítve az UpdateAssessment puszta
/// szerkesztésétől. Nincs Risk-oldali hatása (a Risk.Status ekkor már Active, a korábbi
/// AssessmentRejectedHandler futása óta) — az AssessmentReopened eseménynek jelenleg
/// nincs feliratkozója, tisztán auditálási célt szolgál.
/// </summary>
public sealed class ReopenAssessmentHandler(RiskRegisterDbContext db, ITenantContext tenantContext, IMessageBus messageBus)
{
    [WolverinePost("/api/riskregister/assessments/{id}/reopen")]
    [Authorize(Roles = "RiskOwner")]
    public async Task<IResult> Handle([FromRoute] Guid id, CancellationToken ct)
    {
        var assessment = await db.Assessments
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantContext.TenantId, ct);

        if (assessment is null)
        {
            return Results.NotFound();
        }

        try
        {
            assessment.Reopen();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ex.Message);
        }

        await db.SaveChangesAsync(ct);
        await DomainEventDispatcher.DispatchAsync(assessment, messageBus);

        return Results.NoContent();
    }
}
