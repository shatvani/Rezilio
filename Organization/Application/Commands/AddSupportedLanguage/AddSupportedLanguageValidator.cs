using Rezilio.SharedKernel.DDD.VOs;

namespace Rezilio.Modules.Organization.Application.Commands.AddSupportedLanguage;

public sealed class AddSupportedLanguageValidator : AbstractValidator<AddSupportedLanguageCommand>
{
    public AddSupportedLanguageValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("A TenantId kötelező.");

        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("A nyelvkód kötelező.")
            .Must(BeAValidLanguageCode).WithMessage("Érvénytelen BCP 47 nyelvkód.")
            .When(x => !string.IsNullOrWhiteSpace(x.LanguageCode));
    }

    private static bool BeAValidLanguageCode(string value)
    {
        try { _ = new LanguageCode(value); return true; }
        catch (ArgumentException) { return false; }
    }
}
