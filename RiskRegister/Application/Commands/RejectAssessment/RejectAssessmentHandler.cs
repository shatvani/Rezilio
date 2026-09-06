using Rezilio.Modules.RiskRegister.Application.Services;
using Wolverine;

namespace Rezilio.Modules.RiskRegister.Application.Commands.RejectAssessment;

/// <summary>
/// Jogosultság: RiskManager/Admin (ld. §3.3). Submitted → Rejected — modulon belüli
/// event-handleren keresztül (AssessmentRejectedHandler) dönti el, hogy a Risk.Status
/// Draft-ba vagy Active-ba esik-e vissza (attól függően, volt-e már korábbi jóváhagyott
/// Assessment ugyanahhoz a Risk-hez).
/// </summary>
public sealed class RejectAssessmentHandler(RiskRegisterDbContext db, ITenantContext tenantContext, IMessageBus messageBus)
{
    [WolverinePost("/api/riskregister/assessments/{id}/reject")]
    [Authorize(Roles = "RiskManager,Admin")]
    public async Task<IResult> Handle([FromRoute] Guid id, RejectAssessmentCommand command, CancellationToken ct)
    {
        var assessment = await db.Assessments
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantContext.TenantId, ct);

        if (assessment is null)
        {
            return Results.NotFound();
        }

        try
        {
            assessment.Reject(command.RejectionReason);
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
