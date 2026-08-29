using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.Organization.Domain.Events;

public sealed record BusinessProcessCreated(Guid BusinessProcessId, Guid TenantId) : DomainEvent;
