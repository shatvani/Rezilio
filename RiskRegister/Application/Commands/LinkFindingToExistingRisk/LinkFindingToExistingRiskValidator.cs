namespace Rezilio.Modules.RiskRegister.Application.Commands.LinkFindingToExistingRisk;

public sealed class LinkFindingToExistingRiskValidator : AbstractValidator<LinkFindingToExistingRiskCommand>
{
    public LinkFindingToExistingRiskValidator()
    {
        RuleFor(x => x.RiskId).NotEmpty().WithMessage("A RiskId kötelező.");
    }
}
