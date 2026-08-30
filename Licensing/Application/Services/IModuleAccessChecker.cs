namespace Rezilio.Modules.Licensing.Application.Services;

public interface IModuleAccessChecker
{
    Task<bool> IsModuleActiveAsync(ModuleType module, Guid tenantId, CancellationToken ct = default);
}
