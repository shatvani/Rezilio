using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rezilio.Modules.Organization.Infrastructure;

namespace Rezilio.Modules.Organization;

public static class OrganizationModule
{
    public static IServiceCollection AddOrganizationModule(
        this IServiceCollection services,
        string connectionString)
    {
        var dbContextOptions = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        services.AddSingleton(dbContextOptions);
        services.AddScoped<OrganizationDbContext>();

        return services;
    }
}
