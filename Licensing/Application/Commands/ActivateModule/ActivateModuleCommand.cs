using Rezilio.Modules.Licensing.Domain;

namespace Rezilio.Modules.Licensing.Application.Commands.ActivateModule;

public sealed record ActivateModuleCommand(Guid TenantId, ModuleType Module);
