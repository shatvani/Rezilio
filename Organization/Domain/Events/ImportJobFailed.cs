using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.Organization.Domain.Events;

public sealed record ImportJobFailed(
    Guid ImportJobId,
    Guid TenantId,
    EntityType EntityType,
    string Reason) : DomainEvent;
