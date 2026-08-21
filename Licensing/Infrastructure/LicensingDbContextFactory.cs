using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Rezilio.Modules.Licensing.Infrastructure;

// Az EF Core tools design-time-ban nem tudják felépíteni a DI containert.
// A connection string itt hardcode-olt — ez csak design-time (migration generáláshoz).
// Ez a fájl soha nem kerül éles környezetbe, csak a migration tooling használja.
public sealed class LicensingDbContextFactory : IDesignTimeDbContextFactory<LicensingDbContext>
{
    public LicensingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LicensingDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=rezilio;Username=rezilio;Password=rezilio_dev")
            .Options;

        return new LicensingDbContext(options);
    }
}
