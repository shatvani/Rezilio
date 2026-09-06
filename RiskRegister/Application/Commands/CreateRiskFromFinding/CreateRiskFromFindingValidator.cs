using Rezilio.SharedKernel.Contracts.Organization;
using Wolverine;

namespace Rezilio.Modules.RiskRegister.Application.Commands.CreateRiskFromFinding;

public sealed class CreateRiskFromFindingValidator : AbstractValidator<CreateRiskFromFindingCommand>
{
    public CreateRiskFromFindingValidator(IMessageBus messageBus, ITenantContext tenantContext)
    {
        RuleFor(x => x.Domain).IsInEnum().WithMessage("Érvénytelen RiskDomain érték.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("A Title kötelező.")
            .MaximumLength(200).WithMessage("A Title maximum 200 karakter lehet.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("A Description kötelező.")
            .MaximumLength(2000).WithMessage("A Description maximum 2000 karakter lehet.");

        RuleFor(x => x.OwnerId)
            .MustAsync(async (ownerId, ct) =>
            {
                if (ownerId is null)
                {
                    return true;
                }

                var result = await messageBus.InvokeAsync<KeyPersonLookupResult>(
                    new KeyPersonLookupQuery(tenantContext.TenantId, ownerId.Value), ct);

                return result is { Exists: true, IsActive: true };
            })
            .WithMessage("A megadott OwnerId nem létező vagy nem aktív kulcsszemélyre mutat.");
    }
}
