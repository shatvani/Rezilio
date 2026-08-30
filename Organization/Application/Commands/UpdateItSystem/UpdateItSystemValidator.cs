namespace Rezilio.Modules.Organization.Application.Commands.UpdateItSystem;

public sealed class UpdateItSystemValidator : AbstractValidator<UpdateItSystemCommand>
{
    public UpdateItSystemValidator(OrganizationDbContext db)
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Az Id kötelező.");

        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("A TenantId kötelező.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("A név kötelező.")
            .MaximumLength(200).WithMessage("A név maximum 200 karakter lehet.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("A kód kötelező.")
            .MaximumLength(50).WithMessage("A kód maximum 50 karakter lehet.");

        RuleFor(x => x.Vendor)
            .MaximumLength(200).WithMessage("A gyártó neve maximum 200 karakter lehet.");

        RuleFor(x => x.Version)
            .MaximumLength(100).WithMessage("A verzió maximum 100 karakter lehet.");

        RuleFor(x => x.OwnerId)
            .MustAsync(async (command, ownerId, ct) =>
                ownerId is null ||
                await db.KeyPersons.AnyAsync(k => k.Id == ownerId.Value && k.TenantId == command.TenantId, ct))
            .WithMessage("A megadott Owner nem létező kulcsszemélyre mutat.");
    }
}
