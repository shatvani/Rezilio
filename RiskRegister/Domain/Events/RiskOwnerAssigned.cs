using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.RiskRegister.Domain.Events;

public sealed record RiskOwnerAssigned(Guid RiskId, Guid TenantId, Guid OwnerId) : DomainEvent;
