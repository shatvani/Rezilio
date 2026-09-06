using Rezilio.Modules.RiskRegister.Application.Services;
using Rezilio.SharedKernel.Contracts.Organization;
using Rezilio.SharedKernel.DDD.VOs;
using Wolverine;

namespace Rezilio.Modules.RiskRegister.Application.Commands.UpdateAssessment;

public sealed class UpdateAssessmentHandler(
    RiskRegisterDbContext db,
    ITenantContext tenantContext,
    ICurrentUserContext currentUserContext,
    IMessageBus messageBus)
{
    /// <summary>Jogosultság: RiskOwner. Csak Draft állapotban hívható (ld. §3.3).</summary>
    [WolverinePut("/api/riskregister/assessments/{id}")]
    [Authorize(Roles = "RiskOwner")]
    public async Task<IResult> Handle([FromRoute] Guid id, UpdateAssessmentCommand command, CancellationToken ct)
    {
        var assessment = await db.Assessments
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantContext.TenantId, ct);

        if (assessment is null)
        {
            return Results.NotFound();
        }

        Money? estimatedFinancialImpact = command.EstimatedFinancialImpactAmount is { } amount
            ? new Money(amount, new CurrencyCode(command.EstimatedFinancialImpactCurrency!))
            : null;

        Money? ebitdaBaselineSnapshot = null;
        if (estimatedFinancialImpact is not null)
        {
            var lookup = await messageBus.InvokeAsync<EbitdaBaselineLookupResult>(
                new EbitdaBaselineLookupQuery(tenantContext.TenantId, command.ImpactContextOrgUnitId, command.ImpactContextBusinessProcessId), ct);
            ebitdaBaselineSnapshot = lookup.AnnualEbitda;
        }

        try
        {
            assessment.Update(
                command.InherentLikelihood,
                command.InherentImpact,
                command.ResidualLikelihood,
                command.ResidualImpact,
                command.TargetLikelihood,
                command.TargetImpact,
                estimatedFinancialImpact,
                ebitdaBaselineSnapshot,
                command.ImpactContextOrgUnitId,
                command.ImpactContextBusinessProcessId);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ex.Message);
        }

        assessment.AuditChanges(currentUserContext.Email ?? currentUserContext.UserId ?? "unknown");
        await db.SaveChangesAsync(ct);

        // Update nem vált ki domain eventet (ld. Assessment.Update XML-doksi — a §2.2
        // lezárt event-lista nem tartalmaz AssessmentUpdated-et), de a dispatch-hívás itt
        // is szerepel a konzisztencia és a jövőbeli kiterjeszthetőség kedvéért — ha
        // Update mégis felvenne egy eventet, nem kellene visszamenőleg módosítani a handlert.
        await DomainEventDispatcher.DispatchAsync(assessment, messageBus);

        return Results.NoContent();
    }
}
