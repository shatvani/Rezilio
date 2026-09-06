namespace Rezilio.Modules.Organization.Application.Commands.SetOrganizationalUnitAnnualEbitda;

public sealed class SetOrganizationalUnitAnnualEbitdaValidator : AbstractValidator<SetOrganizationalUnitAnnualEbitdaCommand>
{
    public SetOrganizationalUnitAnnualEbitdaValidator(OrganizationDbContext db, ITenantContext tenantContext)
    {
        RuleFor(x => x.OrgUnitId)
            .NotEmpty().WithMessage("Az OrgUnitId kötelező.")
            .MustAsync(async (orgUnitId, ct) =>
                await db.OrganizationalUnits.AnyAsync(o => o.Id == orgUnitId && o.TenantId == tenantContext.TenantId, ct))
            .WithMessage("A megadott szervezeti egység nem létezik.");

        When(x => x.Amount is not null, () =>
        {
            RuleFor(x => x.Amount!.Value)
                .GreaterThanOrEqualTo(0m).WithMessage("Az éves EBITDA nem lehet negatív.");

            RuleFor(x => x.Currency)
                .NotEmpty().WithMessage("Ha Amount meg van adva, a Currency kötelező.");
        });
    }
}
