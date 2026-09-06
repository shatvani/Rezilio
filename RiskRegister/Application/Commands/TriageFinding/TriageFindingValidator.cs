using Rezilio.SharedKernel.Contracts.Organization;
using Wolverine;

namespace Rezilio.Modules.RiskRegister.Application.Commands.TriageFinding;

public sealed class TriageFindingValidator : AbstractValidator<TriageFindingCommand>
{
    public TriageFindingValidator(IMessageBus messageBus, ITenantContext tenantContext)
    {
        RuleFor(x => x.OwnerId)
            .NotEmpty().WithMessage("Az OwnerId kötelező (ld. §2.2 triázs-szabály).")
            .MustAsync(async (ownerId, ct) =>
            {
                var result = await messageBus.InvokeAsync<KeyPersonLookupResult>(
                    new KeyPersonLookupQuery(tenantContext.TenantId, ownerId), ct);

                return result is { Exists: true, IsActive: true };
            })
            .WithMessage("A megadott OwnerId nem létező vagy nem aktív kulcsszemélyre mutat.");
    }
}
