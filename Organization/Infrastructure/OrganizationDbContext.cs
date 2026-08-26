using Microsoft.EntityFrameworkCore;
using Rezilio.Modules.Organization.Domain;

namespace Rezilio.Modules.Organization.Infrastructure;

public sealed class OrganizationDbContext : DbContext
{
    public OrganizationDbContext(DbContextOptions<OrganizationDbContext> options) : base(options) { }

    public DbSet<TenantSettings> TenantSettings => Set<TenantSettings>();
    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();
    public DbSet<OrganizationalUnit> OrganizationalUnits => Set<OrganizationalUnit>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();

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
            entity.Property(e => e.FileContent).IsRequired();

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

        // --- OrganizationalUnit ---
        modelBuilder.Entity<OrganizationalUnit>(entity =>
        {
            entity.ToTable("organizational_units");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ParentId);
            entity.Property(e => e.Description).HasMaxLength(1000);

            entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
            entity.HasIndex(e => e.TenantId);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Industry).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.ContactEmail).HasMaxLength(200);
            entity.Property(e => e.ContactPhone).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Industry).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.ContactEmail).HasMaxLength(200);
            entity.Property(e => e.ContactPhone).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
        });
    }
}
