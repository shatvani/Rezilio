using Rezilio.SharedKernel.Contracts.Organization;
using Wolverine;

namespace Rezilio.Modules.RiskRegister.Application.Commands.CreateAssessment;

public sealed class CreateAssessmentValidator : AbstractValidator<CreateAssessmentCommand>
{
    public CreateAssessmentValidator(RiskRegisterDbContext db, IMessageBus messageBus, ITenantContext tenantContext)
    {
        RuleFor(x => x.RiskId)
            .NotEmpty().WithMessage("A RiskId kötelező.")
            .MustAsync(async (riskId, ct) =>
            {
                var risk = await db.Risks
                    .Where(r => r.Id == riskId && r.TenantId == tenantContext.TenantId)
                    .Select(r => new { r.Status })
                    .FirstOrDefaultAsync(ct);

                return risk is not null && risk.Status != RiskStatus.Archived;
            })
            .WithMessage("A megadott Risk nem létezik, vagy Archived állapotú (ld. §2.2 invariáns).");

        RuleFor(x => x.RiskId)
            .MustAsync(async (riskId, ct) => !await db.Assessments.AnyAsync(
                a => a.RiskId == riskId && a.TenantId == tenantContext.TenantId
                     && (a.ApprovalStatus == ApprovalStatus.Draft || a.ApprovalStatus == ApprovalStatus.Submitted), ct))
            .WithMessage("Ehhez a Risk-hez már van folyamatban lévő (Draft/Submitted) Assessment — előbb azt kell véglegesíteni.");

        RuleFor(x => x.InherentLikelihood).InclusiveBetween(1, 5).WithMessage("Az InherentLikelihood 1 és 5 között lehet.");
        RuleFor(x => x.InherentImpact).InclusiveBetween(1, 5).WithMessage("Az InherentImpact 1 és 5 között lehet.");
        RuleFor(x => x.ResidualLikelihood).InclusiveBetween(1, 5).WithMessage("A ResidualLikelihood 1 és 5 között lehet.");
        RuleFor(x => x.ResidualImpact).InclusiveBetween(1, 5).WithMessage("A ResidualImpact 1 és 5 között lehet.");

        // FONTOS: a NotNull() és az InclusiveBetween() SZÁNDÉKOSAN külön RuleFor-láncban
        // van — egy FluentValidation .When() alapértelmezetten a teljes megelőző láncra
        // visszahat, tehát ha az InclusiveBetween-hez fűzött .When(x => x.TargetLikelihood
        // is not null) ugyanabban a láncban szerepelne a NotNull() UTÁN, az retroaktívan a
        // NotNull()-t is feltételessé tenné — pont akkor nem futna le a "kötelező pár"
        // ellenőrzés, amikor a másik mező null, ami az egész szabály értelmét venné el.
        When(x => x.TargetLikelihood is not null || x.TargetImpact is not null, () =>
        {
            RuleFor(x => x.TargetLikelihood)
                .NotNull().WithMessage("A TargetLikelihood és TargetImpact csak együtt adható meg.");

            RuleFor(x => x.TargetImpact)
                .NotNull().WithMessage("A TargetLikelihood és TargetImpact csak együtt adható meg.");
        });

        RuleFor(x => x.TargetLikelihood!.Value)
            .InclusiveBetween(1, 5)
            .When(x => x.TargetLikelihood is not null)
            .WithMessage("A TargetLikelihood 1 és 5 között lehet.");

        RuleFor(x => x.TargetImpact!.Value)
            .InclusiveBetween(1, 5)
            .When(x => x.TargetImpact is not null)
            .WithMessage("A TargetImpact 1 és 5 között lehet.");

        When(x => x.EstimatedFinancialImpactAmount is not null, () =>
        {
            RuleFor(x => x.EstimatedFinancialImpactAmount!.Value)
                .GreaterThanOrEqualTo(0m).WithMessage("A becsült pénzügyi hatás nem lehet negatív.");

            RuleFor(x => x.EstimatedFinancialImpactCurrency)
                .NotEmpty().WithMessage("Ha EstimatedFinancialImpactAmount meg van adva, a Currency kötelező.");
        });

        RuleFor(x => x)
            .Must(x => x.ImpactContextOrgUnitId is null || x.ImpactContextBusinessProcessId is null)
            .WithMessage("Az ImpactContextOrgUnitId és ImpactContextBusinessProcessId kölcsönösen kizárják egymást.");

        RuleFor(x => x.AssessedBy)
            .NotEmpty().WithMessage("Az AssessedBy kötelező.")
            .MustAsync(async (assessedBy, ct) =>
            {
                var result = await messageBus.InvokeAsync<KeyPersonLookupResult>(
                    new KeyPersonLookupQuery(tenantContext.TenantId, assessedBy), ct);

                return result is { Exists: true, IsActive: true };
            })
            .WithMessage("A megadott AssessedBy nem létező vagy nem aktív kulcsszemélyre mutat.");
    }
}
