namespace Rezilio.Modules.Organization.Application.Commands.LinkKeyPersonToUser;

public sealed class LinkKeyPersonToUserValidator : AbstractValidator<LinkKeyPersonToUserCommand>
{
    public LinkKeyPersonToUserValidator(OrganizationDbContext db, ITenantContext tenantContext)
    {
        RuleFor(x => x.KeyPersonId)
            .NotEmpty().WithMessage("A KeyPersonId kötelező.")
            .MustAsync(async (keyPersonId, ct) =>
                await db.KeyPersons.AnyAsync(k => k.Id == keyPersonId && k.TenantId == tenantContext.TenantId, ct))
            .WithMessage("A megadott KeyPerson nem létezik.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("A UserId kötelező.")
            .MaximumLength(100).WithMessage("A UserId maximum 100 karakter lehet.");
    }
}
