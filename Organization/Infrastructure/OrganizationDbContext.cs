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

        modelBuilder.Entity<ImportJob>(entity =>
        {
            entity.ToTable("import_jobs");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.EntityType)
                  .HasConversion<string>()      // enum → VARCHAR
                  .IsRequired();
            entity.Property(e => e.Status)
                  .HasConversion<string>()
                  .IsRequired();

            entity.Property(e => e.TotalRows).IsRequired();
            entity.Property(e => e.SuccessRows).IsRequired();
            entity.Property(e => e.ErrorRows).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CompletedAt);

            // ImportRowResult lista → JSONB (PostgreSQL)
            entity.OwnsMany(e => e.Results, owned =>
            {
                owned.ToJson("results");
                owned.Property(r => r.RowNumber).IsRequired();
                owned.Property(r => r.IsSuccess).IsRequired();
                owned.Property(r => r.ErrorMessage);
                owned.Property(r => r.ColumnName);
            });

            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.EntityType });
        });
    }
}
