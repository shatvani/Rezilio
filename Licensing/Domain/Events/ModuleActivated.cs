using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.Licensing.Domain.Events;

public sealed record ModuleActivated(Guid TenantId, ModuleType Module) : DomainEvent;
