using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.RiskRegister.Domain.Events;

public sealed record FindingTriaged(Guid FindingId, Guid TenantId, Guid OwnerId) : DomainEvent;
