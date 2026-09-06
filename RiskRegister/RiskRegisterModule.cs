using Microsoft.Extensions.DependencyInjection;

namespace Rezilio.Modules.RiskRegister;

public static class RiskRegisterModule
{
    public static IServiceCollection AddRiskRegisterModule(
        this IServiceCollection services,
        string connectionString)
    {
        var dbContextOptions = new DbContextOptionsBuilder<RiskRegisterDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        services.AddSingleton(dbContextOptions);
        services.AddScoped<RiskRegisterDbContext>();

        return services;
    }
}
