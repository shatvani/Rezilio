using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.Organization.Domain.Events;

public sealed record ImportJobCompleted(
    Guid ImportJobId,
    Guid TenantId,
    EntityType EntityType,
    int TotalRows,
    int SuccessRows) : DomainEvent;
