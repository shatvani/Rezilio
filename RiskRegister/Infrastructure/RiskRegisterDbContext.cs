namespace Rezilio.Modules.RiskRegister.Infrastructure;

public sealed class RiskRegisterDbContext : DbContext
{
    public RiskRegisterDbContext(DbContextOptions<RiskRegisterDbContext> options) : base(options) { }

    public DbSet<Risk> Risks => Set<Risk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Risk>(entity =>
        {
            entity.ToTable("risks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Domain).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(e => e.CreatedBy).HasMaxLength(200);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(200);
            entity.Property(e => e.CorrelationId).HasMaxLength(100);

            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.HasIndex(e => new { e.TenantId, e.OwnerId });

            // Önhivatkozó FK (CreateRiskFromArchivedRisk audit-nyom, ld. §2.2/§2.6) —
            // Restrict: egy forrás Risk nem törölhető (amúgy sincs hard delete a modulban),
            // amíg rá hivatkozó másolat létezik.
            entity.HasOne<Risk>()
                  .WithMany()
                  .HasForeignKey(e => e.CopiedFromRiskId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
