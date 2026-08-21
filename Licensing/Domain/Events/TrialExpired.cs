using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.Licensing.Domain.Events;

public sealed record TrialExpired(Guid TenantId, ModuleType Module) : DomainEvent;
