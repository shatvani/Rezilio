using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Organization.Application.Services;
using Rezilio.Modules.Organization.Application.Services;
using Rezilio.Modules.Organization.Infrastructure;
using Rezilio.Modules.Organization.Infrastructure.Excel;

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

        // Excel infrastruktúra
        services.AddScoped<IExcelTemplateGenerator, ExcelTemplateGenerator>();
        services.AddScoped<IExcelImportParser, ExcelImportParser>();

        // Column definition providerek
        services.AddScoped<IImportColumnDefinitionProvider, OrganizationalUnitColumnDefinitionProvider>();
        services.AddScoped<IImportColumnDefinitionProvider, CustomerColumnDefinitionProvider>();
        services.AddScoped<IImportColumnDefinitionProvider, SupplierColumnDefinitionProvider>();
        services.AddScoped<IImportColumnDefinitionProvider, KeyPersonColumnDefinitionProvider>();
        services.AddScoped<IImportColumnDefinitionProvider, ItSystemColumnDefinitionProvider>();

        return services;
    }
}
