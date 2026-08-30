namespace Rezilio.Modules.Licensing.Application.Commands.DeactivateModule;

public sealed record DeactivateModuleCommand(Guid TenantId, ModuleType Module);
