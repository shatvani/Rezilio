using Microsoft.EntityFrameworkCore.Design;

namespace Rezilio.Modules.Organization.Infrastructure;

public sealed class OrganizationDbContextFactory : IDesignTimeDbContextFactory<OrganizationDbContext>
{
    public OrganizationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=rezilio;Username=rezilio;Password=rezilio_dev")
            .Options;

        return new OrganizationDbContext(options);
    }
}
