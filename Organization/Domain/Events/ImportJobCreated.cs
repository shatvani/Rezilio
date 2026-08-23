using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.Organization.Domain.Events;

public sealed record ImportJobCreated(
    Guid ImportJobId,
    Guid TenantId,
    EntityType EntityType) : DomainEvent;
