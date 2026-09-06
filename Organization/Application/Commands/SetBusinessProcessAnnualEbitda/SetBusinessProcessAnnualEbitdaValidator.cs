namespace Rezilio.Modules.Organization.Application.Commands.SetBusinessProcessAnnualEbitda;

public sealed class SetBusinessProcessAnnualEbitdaValidator : AbstractValidator<SetBusinessProcessAnnualEbitdaCommand>
{
    public SetBusinessProcessAnnualEbitdaValidator(OrganizationDbContext db, ITenantContext tenantContext)
    {
        RuleFor(x => x.BusinessProcessId)
            .NotEmpty().WithMessage("A BusinessProcessId kötelező.")
            .MustAsync(async (businessProcessId, ct) =>
                await db.BusinessProcesses.AnyAsync(b => b.Id == businessProcessId && b.TenantId == tenantContext.TenantId, ct))
            .WithMessage("A megadott üzleti folyamat nem létezik.");

        When(x => x.Amount is not null, () =>
        {
            RuleFor(x => x.Amount!.Value)
                .GreaterThanOrEqualTo(0m).WithMessage("Az éves EBITDA nem lehet negatív.");

            RuleFor(x => x.Currency)
                .NotEmpty().WithMessage("Ha Amount meg van adva, a Currency kötelező.");
        });
    }
}
