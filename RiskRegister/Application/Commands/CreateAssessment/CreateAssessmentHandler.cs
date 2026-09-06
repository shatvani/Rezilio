using Rezilio.Modules.RiskRegister.Application.Services;
using Rezilio.SharedKernel.Contracts.Organization;
using Rezilio.SharedKernel.DDD.VOs;
using Wolverine;

namespace Rezilio.Modules.RiskRegister.Application.Commands.CreateAssessment;

public sealed class CreateAssessmentHandler(
    RiskRegisterDbContext db,
    ITenantContext tenantContext,
    ICurrentUserContext currentUserContext,
    IMessageBus messageBus)
{
    /// <summary>Jogosultság: RiskOwner (ld. §1.7/§3.3 — az elemzést Risk Owner + szakértő végzi).</summary>
    [WolverinePost("/api/riskregister/assessments")]
    [Authorize(Roles = "RiskOwner")]
    public async Task<IResult> Handle(CreateAssessmentCommand command, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId;

        Money? estimatedFinancialImpact = command.EstimatedFinancialImpactAmount is { } amount
            ? new Money(amount, new CurrencyCode(command.EstimatedFinancialImpactCurrency!))
            : null;

        // A baseline-lekérdezés csak akkor szükséges, ha van mihez viszonyítani (ld. §3.3
        // — a RiskRegister nem fésül össze két külön Organization-lekérdezést, hanem egy
        // konszolidált EbitdaBaselineLookupQuery-t hív).
        Money? ebitdaBaselineSnapshot = null;
        if (estimatedFinancialImpact is not null)
        {
            var lookup = await messageBus.InvokeAsync<EbitdaBaselineLookupResult>(
                new EbitdaBaselineLookupQuery(tenantId, command.ImpactContextOrgUnitId, command.ImpactContextBusinessProcessId), ct);
            ebitdaBaselineSnapshot = lookup.AnnualEbitda;
        }

        var inputs = new AssessmentScoreInputs(
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

        Assessment assessment;
        try
        {
            assessment = Assessment.Create(tenantId, command.RiskId, inputs, command.AssessedBy);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ex.Message);
        }

        assessment.CreatedAt = DateTimeOffset.UtcNow;
        assessment.CreatedBy = currentUserContext.Email ?? currentUserContext.UserId ?? "unknown";

        db.Assessments.Add(assessment);
        await db.SaveChangesAsync(ct);

        // Ld. §2.5 esemény-dispatch minta: SaveChanges UTÁN, explicit ClearDomainEvents +
        // PublishAsync — nincs automatikus interceptor a kódbázisban (ld.
        // DomainEventDispatcher XML-doksi a dokumentált v1-korlátozásért).
        await DomainEventDispatcher.DispatchAsync(assessment, messageBus);

        return Results.Created($"/api/riskregister/assessments/{assessment.Id}", new { assessment.Id });
    }
}
