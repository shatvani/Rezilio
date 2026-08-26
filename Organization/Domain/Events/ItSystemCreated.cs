using Rezilio.SharedKernel.DDD;

namespace Organization.Domain.Events;

public sealed record ItSystemCreated(Guid ItSystemId, Guid TenantId) : DomainEvent;
