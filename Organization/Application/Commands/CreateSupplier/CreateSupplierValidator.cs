using FluentValidation;

namespace Rezilio.Modules.Organization.Application.Commands.CreateSupplier;

public sealed class CreateSupplierValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("A TenantId kötelező.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("A név kötelező.")
            .MaximumLength(200).WithMessage("A név maximum 200 karakter lehet.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("A kód kötelező.")
            .MaximumLength(50).WithMessage("A kód maximum 50 karakter lehet.");

        RuleFor(x => x.Industry)
            .MaximumLength(100).WithMessage("Az iparág maximum 100 karakter lehet.");

        RuleFor(x => x.Country)
            .MaximumLength(100).WithMessage("Az ország maximum 100 karakter lehet.");

        RuleFor(x => x.ContactEmail)
            .EmailAddress().WithMessage("Érvénytelen email cím.")
            .MaximumLength(200).WithMessage("Az email cím maximum 200 karakter lehet.")
            .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));

        RuleFor(x => x.ContactPhone)
            .MaximumLength(50).WithMessage("A telefonszám maximum 50 karakter lehet.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("A leírás maximum 500 karakter lehet.");
    }
}
