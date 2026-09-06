using Rezilio.Modules.RiskRegister.Application.Services;
using Wolverine;

namespace Rezilio.Modules.RiskRegister.Application.Commands.SubmitAssessment;

/// <summary>
/// Jogosultság: RiskOwner. Draft → Submitted (ld. §3.3) — modulon belüli event-handleren
/// keresztül (AssessmentSubmittedHandler) indítja el a Risk.Status → UnderReview átmenetet.
/// </summary>
public sealed class SubmitAssessmentHandler(RiskRegisterDbContext db, ITenantContext tenantContext, IMessageBus messageBus)
{
    [WolverinePost("/api/riskregister/assessments/{id}/submit")]
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
            assessment.Submit();
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
