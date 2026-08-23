using Rezilio.Modules.Organization.Domain.Events;
using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.Organization.Domain;

public sealed class ImportJob : AggregateRoot<Guid>
{
    public Guid TenantId { get; private set; }
    public EntityType EntityType { get; private set; }
    public ImportJobStatus Status { get; private set; }
    public int TotalRows { get; private set; }
    public int SuccessRows { get; private set; }
    public int ErrorRows { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private readonly List<ImportRowResult> _results = [];
    public IReadOnlyList<ImportRowResult> Results => _results.AsReadOnly();

    // EF Core proxy ctor
    private ImportJob() { }

    public static ImportJob Create(Guid tenantId, EntityType entityType)
    {
        var job = new ImportJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EntityType = entityType,
            Status = ImportJobStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        job.RaiseDomainEvent(new ImportJobCreated(job.Id, tenantId, entityType));
        return job;
    }

    /// <summary>Validáció megkezdése.</summary>
    public void StartValidation()
    {
        EnsureStatus(ImportJobStatus.Pending);
        Status = ImportJobStatus.Validating;
    }

    /// <summary>Validáció eredménye: sikeres sorok + hibás sorok.</summary>
    public void CompleteValidation(IEnumerable<ImportRowResult> results)
    {
        EnsureStatus(ImportJobStatus.Validating);

        _results.AddRange(results);
        TotalRows = _results.Count;
        SuccessRows = _results.Count(r => r.IsSuccess);
        ErrorRows = _results.Count(r => !r.IsSuccess);

        Status = ErrorRows == 0 ? ImportJobStatus.Valid : ImportJobStatus.Invalid;
    }

    /// <summary>Tényleges adatbetöltés megkezdése — csak Valid állapotból.</summary>
    public void StartImport()
    {
        EnsureStatus(ImportJobStatus.Valid);
        Status = ImportJobStatus.Importing;
    }

    /// <summary>Import sikeresen lezárult.</summary>
    public void Complete()
    {
        EnsureStatus(ImportJobStatus.Importing);
        Status = ImportJobStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new ImportJobCompleted(Id, TenantId, EntityType, TotalRows, SuccessRows));
    }

    /// <summary>Import váratlan hibával meghiúsult.</summary>
    public void Fail(string reason)
    {
        if (Status is not (ImportJobStatus.Validating or ImportJobStatus.Importing))
        {
            throw new InvalidOperationException(
                $"ImportJob only fails from Validating or Importing state. Current: {Status}");
        }

        Status = ImportJobStatus.Failed;
        CompletedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new ImportJobFailed(Id, TenantId, EntityType, reason));
    }

    private void EnsureStatus(ImportJobStatus expected)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException(
                $"Expected status {expected}, but current status is {Status}.");
        }
    }
}
