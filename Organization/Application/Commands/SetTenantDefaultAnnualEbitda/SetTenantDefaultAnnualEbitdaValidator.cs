namespace Rezilio.Modules.Organization.Application.Commands.SetTenantDefaultAnnualEbitda;

public sealed class SetTenantDefaultAnnualEbitdaValidator : AbstractValidator<SetTenantDefaultAnnualEbitdaCommand>
{
    public SetTenantDefaultAnnualEbitdaValidator()
    {
        // Amount==null => törlés, nincs egyéb megkötés.
        When(x => x.Amount is not null, () =>
        {
            RuleFor(x => x.Amount!.Value)
                .GreaterThanOrEqualTo(0m).WithMessage("Az éves EBITDA nem lehet negatív.");

            RuleFor(x => x.Currency)
                .NotEmpty().WithMessage("Ha Amount meg van adva, a Currency kötelező.");
            // A pénznem-kód formai validációja (ISO 4217) magában a CurrencyCode VO-ban
            // történik a handlerben — itt csak az üresség-ellenőrzés a feladat.
        });
    }
}
