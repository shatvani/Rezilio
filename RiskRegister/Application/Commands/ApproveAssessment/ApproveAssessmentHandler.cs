using Rezilio.Modules.RiskRegister.Application.Services;
using Wolverine;

namespace Rezilio.Modules.RiskRegister.Application.Commands.ApproveAssessment;

/// <summary>
/// Jogosultság: RiskManager/Admin (ld. §3.3). Submitted → Approved — modulon belüli
/// event-handleren keresztül (AssessmentApprovedHandler) indítja el a Risk.Status → Active
/// átmenetet ÉS a modulhatáron átnyúló RiskScoreChanged eseményt.
/// </summary>
public sealed class ApproveAssessmentHandler(
    RiskRegisterDbContext db,
    ITenantContext tenantContext,
    ICurrentUserContext currentUserContext,
    IMessageBus messageBus)
{
    [WolverinePost("/api/riskregister/assessments/{id}/approve")]
    [Authorize(Roles = "RiskManager,Admin")]
    public async Task<IResult> Handle([FromRoute] Guid id, CancellationToken ct)
    {
        var assessment = await db.Assessments
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantContext.TenantId, ct);

        if (assessment is null)
        {
            return Results.NotFound();
        }

        var approvedBy = await currentUserContext.GetKeyPersonIdAsync(ct);
        if (approvedBy is null)
        {
            // Ld. ICurrentUserContext XML-doksi — nem minden bejelentkezett user (pl.
            // tisztán Admin realm role-lal, KeyPerson-kötés nélkül) rendelkezik KeyPersonId-vel.
            // Az ApprovedBy viszont kötelező mező az Assessment-en, ezért itt elutasítjuk.
            return Results.BadRequest(
                "A jóváhagyó felhasználóhoz nem tartozik KeyPerson — az Admin előbb kösse össze (LinkKeyPersonToUser).");
        }

        try
        {
            assessment.Approve(approvedBy.Value);
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
