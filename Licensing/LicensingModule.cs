using Microsoft.Extensions.DependencyInjection;
using Rezilio.Modules.Licensing.Application.Services;

namespace Rezilio.Modules.Licensing;

public static class LicensingModule
{
    public static IServiceCollection AddLicensingModule(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<LicensingDbContext>(opts =>
            opts.UseNpgsql(connectionString));

        services.AddScoped<IModuleAccessChecker, ModuleAccessChecker>();

        return services;
    }
}
