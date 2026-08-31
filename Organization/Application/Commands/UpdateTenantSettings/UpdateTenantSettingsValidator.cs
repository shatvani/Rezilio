using Rezilio.SharedKernel.DDD.VOs;

namespace Rezilio.Modules.Organization.Application.Commands.UpdateTenantSettings;

public sealed class UpdateTenantSettingsValidator : AbstractValidator<UpdateTenantSettingsCommand>
{
    public UpdateTenantSettingsValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("A TenantId kötelező.");

        RuleFor(x => x.DefaultCurrency)
            .NotEmpty().WithMessage("A pénznem kód kötelező.")
            .Must(BeAValidCurrencyCode).WithMessage("Érvénytelen ISO 4217 pénznem kód.")
            .When(x => !string.IsNullOrWhiteSpace(x.DefaultCurrency));

        RuleFor(x => x.DefaultLanguage)
            .NotEmpty().WithMessage("Az alapértelmezett nyelv kötelező.")
            .Must(BeAValidLanguageCode).WithMessage("Érvénytelen BCP 47 nyelvkód.")
            .When(x => !string.IsNullOrWhiteSpace(x.DefaultLanguage));

        RuleFor(x => x.Locale)
            .NotEmpty().WithMessage("A területi beállítás (locale) kötelező.")
            .MaximumLength(20).WithMessage("A területi beállítás maximum 20 karakter lehet.");

        RuleFor(x => x.TimeZone)
            .NotEmpty().WithMessage("Az időzóna kötelező.")
            .Must(BeAValidTimeZone).WithMessage("Érvénytelen időzóna azonosító.")
            .When(x => !string.IsNullOrWhiteSpace(x.TimeZone));
    }

    private static bool BeAValidCurrencyCode(string value)
    {
        try { _ = new CurrencyCode(value); return true; }
        catch (ArgumentException) { return false; }
    }

    private static bool BeAValidLanguageCode(string value)
    {
        try { _ = new LanguageCode(value); return true; }
        catch (ArgumentException) { return false; }
    }

    private static bool BeAValidTimeZone(string value)
    {
        try { TimeZoneInfo.FindSystemTimeZoneById(value); return true; }
        catch (TimeZoneNotFoundException) { return false; }
        catch (InvalidTimeZoneException) { return false; }
    }
}
