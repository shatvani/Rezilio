using Microsoft.EntityFrameworkCore;
using Rezilio.Modules.Organization.Domain;

namespace Rezilio.Modules.Organization.Infrastructure;

public sealed class OrganizationDbContext : DbContext
{
    public OrganizationDbContext(DbContextOptions<OrganizationDbContext> options) : base(options) { }

    public DbSet<TenantSettings> TenantSettings => Set<TenantSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantSettings>(entity =>
        {
            entity.ToTable("tenant_settings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.DefaultCurrency)
                  .HasConversion(v => v.Value, v => new(v))
                  .IsRequired();
            entity.Property(e => e.DefaultLanguage)
                  .HasConversion(v => v.Value, v => new(v))
                  .IsRequired();
            entity.Property(e => e.Locale).IsRequired();
            entity.Property(e => e.TimeZone).IsRequired();

            entity.OwnsMany(e => e.SupportedLanguages, owned =>
            {
                owned.ToJson("supported_languages");
                owned.Property(l => l.Value).IsRequired();
            });
        });
    }
}
