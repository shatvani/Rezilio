using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.Organization.Domain.Events;

public sealed record ItSystemCreated(Guid ItSystemId, Guid TenantId) : DomainEvent;
