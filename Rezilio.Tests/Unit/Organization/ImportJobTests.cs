using Rezilio.Modules.Organization.Domain;
using Xunit;
using EntityType = Rezilio.Modules.Organization.Domain.EntityType;

namespace Rezilio.Tests.Unit.Organization;

public class ImportJobTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    // --- Create ---

    [Fact]
    [Trait("Category", "Unit")]
    public void Create_SetsStatusToPending_AndRaisesImportJobCreatedEvent()
    {
        ImportJob job = ImportJob.Create(TenantId, EntityType.OrganizationalUnit, []);

        Assert.Equal(ImportJobStatus.Pending, job.Status);
        Assert.Equal(TenantId, job.TenantId);
        Assert.Equal(EntityType.OrganizationalUnit, job.EntityType);
        Assert.Single(job.DomainEvents);
    }

    // --- Validáció: boldog út ---

    [Fact]
    [Trait("Category", "Unit")]
    public void CompleteValidation_WithNoErrors_SetsStatusToValid()
    {
        ImportJob job = ImportJob.Create(TenantId, EntityType.Location, []);
        job.StartValidation();

        var results = new[]
        {
            new ImportRowResult(1, IsSuccess: true),
            new ImportRowResult(2, IsSuccess: true),
        };

        job.CompleteValidation(results);

        Assert.Equal(ImportJobStatus.Valid, job.Status);
        Assert.Equal(2, job.TotalRows);
        Assert.Equal(2, job.SuccessRows);
        Assert.Equal(0, job.ErrorRows);
    }

    // --- Validáció: hibás sorok ---

    [Fact]
    [Trait("Category", "Unit")]
    public void CompleteValidation_WithErrors_SetsStatusToInvalid()
    {
        ImportJob job = ImportJob.Create(TenantId, EntityType.Customer, []);
        job.StartValidation();

        var results = new[]
        {
            new ImportRowResult(1, IsSuccess: true),
            new ImportRowResult(2, IsSuccess: false, ErrorMessage: "Kötelező mező hiányzik", ColumnName: "Name"),
        };

        job.CompleteValidation(results);

        Assert.Equal(ImportJobStatus.Invalid, job.Status);
        Assert.Equal(1, job.ErrorRows);
    }

    // --- Import: boldog út ---

    [Fact]
    [Trait("Category", "Unit")]
    public void Complete_AfterImporting_SetsStatusToCompleted_AndRaisesEvent()
    {
        ImportJob job = ImportJob.Create(TenantId, EntityType.Supplier, []);
        job.StartValidation();
        job.CompleteValidation([new ImportRowResult(1, IsSuccess: true)]);
        job.StartImport();
        job.Complete();

        Assert.Equal(ImportJobStatus.Completed, job.Status);
        Assert.NotNull(job.CompletedAt);
        Assert.Contains(job.DomainEvents, e => e.GetType().Name == "ImportJobCompleted");
    }

    // --- Fail ---

    [Fact]
    [Trait("Category", "Unit")]
    public void Fail_DuringImporting_SetsStatusToFailed_AndRaisesEvent()
    {
        ImportJob job = ImportJob.Create(TenantId, EntityType.ItSystem, []);
        job.StartValidation();
        job.CompleteValidation([new ImportRowResult(1, IsSuccess: true)]);
        job.StartImport();
        job.Fail("Adatbázis kapcsolat megszakadt");

        Assert.Equal(ImportJobStatus.Failed, job.Status);
        Assert.Contains(job.DomainEvents, e => e.GetType().Name == "ImportJobFailed");
    }

    // --- Státuszgép védelem ---

    [Fact]
    [Trait("Category", "Unit")]
    public void StartImport_FromInvalidStatus_ThrowsInvalidOperationException()
    {
        ImportJob job = ImportJob.Create(TenantId, EntityType.BusinessProcess, []);
        job.StartValidation();
        job.CompleteValidation([new ImportRowResult(1, IsSuccess: false, ErrorMessage: "Hiba")]);

        // Invalid státuszból nem lehet importálni
        Assert.Throws<InvalidOperationException>(() => job.StartImport());
    }
}
