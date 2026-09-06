namespace Rezilio.Modules.RiskRegister.Application.Commands.UpdateRisk;

public sealed class UpdateRiskValidator : AbstractValidator<UpdateRiskCommand>
{
    public UpdateRiskValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("A Title kötelező.")
            .MaximumLength(200).WithMessage("A Title maximum 200 karakter lehet.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("A Description kötelező.")
            .MaximumLength(2000).WithMessage("A Description maximum 2000 karakter lehet.");
    }
}
