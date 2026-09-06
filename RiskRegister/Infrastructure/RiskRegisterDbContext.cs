namespace Rezilio.Modules.RiskRegister.Infrastructure;

public sealed class RiskRegisterDbContext : DbContext
{
    public RiskRegisterDbContext(DbContextOptions<RiskRegisterDbContext> options) : base(options) { }

    public DbSet<Risk> Risks => Set<Risk>();
    public DbSet<Finding> Findings => Set<Finding>();
    public DbSet<Assessment> Assessments => Set<Assessment>();

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

        modelBuilder.Entity<Finding>(entity =>
        {
            entity.ToTable("findings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Source).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Severity).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(e => e.CreatedBy).HasMaxLength(200);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(200);
            entity.Property(e => e.CorrelationId).HasMaxLength(100);

            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.HasIndex(e => new { e.TenantId, e.OwnerId });

            // LinkedRiskId FK a Risk-re — Restrict, mert nincs hard delete a modulban.
            entity.HasOne<Risk>()
                  .WithMany()
                  .HasForeignKey(e => e.LinkedRiskId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Assessment>(entity =>
        {
            entity.ToTable("assessments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.RiskId).IsRequired();

            entity.Property(e => e.InherentLikelihood).IsRequired();
            entity.Property(e => e.InherentImpact).IsRequired();
            entity.Property(e => e.InherentScore).IsRequired();
            entity.Property(e => e.ResidualLikelihood).IsRequired();
            entity.Property(e => e.ResidualImpact).IsRequired();
            entity.Property(e => e.ResidualScore).IsRequired();
            entity.Property(e => e.TargetLikelihood);
            entity.Property(e => e.TargetImpact);
            entity.Property(e => e.TargetScore);

            entity.Property(e => e.EbitdaImpactPercentage).HasColumnType("numeric(9,2)");

            entity.Property(e => e.ImpactContextOrgUnitId);
            entity.Property(e => e.ImpactContextBusinessProcessId);
            entity.Property(e => e.AssessedBy).IsRequired();

            entity.Property(e => e.ApprovalStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(e => e.ApprovedBy);
            entity.Property(e => e.ApprovedAt);
            entity.Property(e => e.RejectionReason).HasMaxLength(2000);

            entity.Property(e => e.CreatedBy).HasMaxLength(200);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(200);
            entity.Property(e => e.CorrelationId).HasMaxLength(100);

            // Money (első EF-perzisztálása a kódbázisban, ld. docs/design/risk-register-design.md
            // §2.2/§3.3, 2026-09-06) — nullable owned reference type-ként mappelve: ha a
            // CLR-oldali Money? érték null, mindkét oszlop NULL marad (EF Core 8+/10 optional
            // owned dependent).
            entity.OwnsOne(e => e.EstimatedFinancialImpact, money =>
            {
                money.Property(m => m.Amount).HasColumnName("estimated_financial_impact_amount");
                money.Property(m => m.Currency)
                     .HasConversion(v => v.Value, v => new(v))
                     .HasColumnName("estimated_financial_impact_currency");
            });

            entity.OwnsOne(e => e.EbitdaBaselineSnapshot, money =>
            {
                money.Property(m => m.Amount).HasColumnName("ebitda_baseline_snapshot_amount");
                money.Property(m => m.Currency)
                     .HasConversion(v => v.Value, v => new(v))
                     .HasColumnName("ebitda_baseline_snapshot_currency");
            });

            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.RiskId });
            entity.HasIndex(e => new { e.TenantId, e.ApprovalStatus });

            // RiskId FK — Restrict, mert nincs hard delete a modulban (ld. Finding/Risk minta).
            entity.HasOne<Risk>()
                  .WithMany()
                  .HasForeignKey(e => e.RiskId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
