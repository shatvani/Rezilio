namespace Rezilio.Modules.Organization.Application.Commands.CreateBusinessProcess;

public sealed class CreateBusinessProcessValidator : AbstractValidator<CreateBusinessProcessCommand>
{
    public CreateBusinessProcessValidator(OrganizationDbContext db, ITenantContext tenantContext)
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("A TenantId kötelező.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("A név kötelező.")
            .MaximumLength(200).WithMessage("A név maximum 200 karakter lehet.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("A kód kötelező.")
            .MaximumLength(50).WithMessage("A kód maximum 50 karakter lehet.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("A kategória kötelező.")
            .MaximumLength(100).WithMessage("A kategória maximum 100 karakter lehet.");

        RuleFor(x => x.OwnerId)
            .MustAsync(async (ownerId, ct) =>
                ownerId is null ||
                await db.KeyPersons.AnyAsync(k => k.Id == ownerId.Value && k.TenantId == tenantContext.TenantId, ct))
            .WithMessage("A megadott Owner nem létező kulcsszemélyre mutat.");
    }
}
