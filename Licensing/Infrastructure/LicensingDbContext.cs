using Microsoft.EntityFrameworkCore;
using Rezilio.Modules.Licensing.Domain;

namespace Rezilio.Modules.Licensing.Infrastructure;

public sealed class LicensingDbContext : DbContext
{
    public LicensingDbContext(DbContextOptions<LicensingDbContext> options) : base(options) { }

    public DbSet<TenantLicense> Licenses => Set<TenantLicense>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantLicense>(entity =>
        {
            entity.ToTable("tenant_licenses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Plan)
                  .HasConversion<string>()
                  .IsRequired();
            entity.Property(e => e.PlanExpiresAt);

            // ModuleAccess value object-ek JSONB oszlopban (PostgreSQL)
            entity.OwnsMany(e => e.ModuleAccesses, owned =>
            {
                owned.ToJson("module_accesses");
                owned.Property(m => m.Module).HasConversion<string>();
                owned.Property(m => m.IsActive);
                owned.Property(m => m.TrialEndsAt);
            });
        });
    }
}
