using Rezilio.SharedKernel.DDD;

namespace Organization.Domain.Events;

public sealed record BusinessProcessCreated(Guid BusinessProcessId, Guid TenantId) : DomainEvent;
