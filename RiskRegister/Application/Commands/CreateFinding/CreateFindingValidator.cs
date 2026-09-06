namespace Rezilio.Modules.RiskRegister.Application.Commands.CreateFinding;

public sealed class CreateFindingValidator : AbstractValidator<CreateFindingCommand>
{
    public CreateFindingValidator()
    {
        RuleFor(x => x.Source).IsInEnum().WithMessage("Érvénytelen FindingSource érték.");
        RuleFor(x => x.Severity).IsInEnum().WithMessage("Érvénytelen FindingSeverity érték.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("A Description kötelező.")
            .MaximumLength(2000).WithMessage("A Description maximum 2000 karakter lehet.");
    }
}
