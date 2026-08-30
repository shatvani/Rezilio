using FluentValidation;

namespace Rezilio.Modules.Licensing.Application.Commands.CreateTenantLicense;

public sealed class CreateTenantLicenseValidator : AbstractValidator<CreateTenantLicenseCommand>
{
    public CreateTenantLicenseValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("A TenantId kötelező.");

        RuleFor(x => x.Plan)
            .IsInEnum().WithMessage("Érvénytelen Plan érték.");
    }
}
