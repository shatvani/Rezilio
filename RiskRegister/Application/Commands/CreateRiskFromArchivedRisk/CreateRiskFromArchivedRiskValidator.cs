namespace Rezilio.Modules.RiskRegister.Application.Commands.CreateRiskFromArchivedRisk;

public sealed class CreateRiskFromArchivedRiskValidator : AbstractValidator<CreateRiskFromArchivedRiskCommand>
{
    public CreateRiskFromArchivedRiskValidator()
    {
        RuleFor(x => x.Domain)
            .IsInEnum().When(x => x.Domain is not null)
            .WithMessage("Érvénytelen RiskDomain érték.");

        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("A Title maximum 200 karakter lehet.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("A Description maximum 2000 karakter lehet.");
    }
}
