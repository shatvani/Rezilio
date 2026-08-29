using FluentValidation;

namespace Rezilio.Modules.Organization.Application.Commands.UpdateKeyPerson;

public sealed class UpdateKeyPersonValidator : AbstractValidator<UpdateKeyPersonCommand>
{
    public UpdateKeyPersonValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Az Id kötelező.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("A név kötelező.")
            .MaximumLength(200).WithMessage("A név maximum 200 karakter lehet.");

        RuleFor(x => x.Title)
            .MaximumLength(100).WithMessage("A beosztás maximum 100 karakter lehet.");

        RuleFor(x => x.Department)
            .MaximumLength(100).WithMessage("Az osztály maximum 100 karakter lehet.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Érvénytelen email cím.")
            .MaximumLength(200).WithMessage("Az email cím maximum 200 karakter lehet.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Phone)
            .MaximumLength(50).WithMessage("A telefonszám maximum 50 karakter lehet.");

        RuleFor(x => x.BackupPersonName)
            .MaximumLength(200).WithMessage("A helyettesítő neve maximum 200 karakter lehet.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("A leírás maximum 500 karakter lehet.");
    }
}
