namespace Rezilio.SharedKernel.DDD;

/// <summary>
/// Auditálható entitás szerződése — csak olvasható felület.
///
/// Miért interfész és nem csak az AuditableAggregateRoot mezői?
/// - Az interfész lehetővé teszi, hogy generikus infrastruktúra kód
///   (pl. EF Core SaveChanges interceptor) típus szerint szűrhessen:
///     if (entry.Entity is IAuditableEntity auditable) { ... }
///   Az interceptor így automatikusan kitölti a CreatedAt/LastModified mezőket
///   minden auditálható entitásnál, anélkül, hogy ismerné a konkrét típust.
/// </summary>
public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; set; }
    string? CreatedBy { get; set; }
    DateTimeOffset? LastModified { get; set; }
    string? LastModifiedBy { get; set; }
    string? CorrelationId { get; set; }
}

