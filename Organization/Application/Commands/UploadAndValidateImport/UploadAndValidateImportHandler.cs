using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rezilio.Modules.Organization.Application.Services;
using Rezilio.Modules.Organization.Domain;
using Rezilio.Modules.Organization.Infrastructure;
using Rezilio.SharedKernel.Multitenancy;
using Wolverine.Http;

namespace Rezilio.Modules.Organization.Application.Commands.UploadAndValidateImport;

public sealed record ImportJobCreatedResponse(Guid JobId, string Status, int TotalRows, int ErrorRows);

public sealed class UploadAndValidateImportHandler(
    OrganizationDbContext db,
    IExcelImportParser parser,
    ITenantContext tenantContext)
{
    [WolverinePost("/api/organization/import/{entityType}/upload")]
    [Authorize]
    public async Task<IResult> Handle(
        [FromRoute] EntityType entityType,
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest("Fájl nem lett feltöltve.");
        }

        // Fájl byte[] konverzió
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        byte[] fileContent = ms.ToArray();

        // ImportJob létrehozása
        Guid tenantId = tenantContext.TenantId;
        ImportJob job = ImportJob.Create(tenantId, entityType);
        db.ImportJobs.Add(job);
        await db.SaveChangesAsync(ct);

        // Validáció
        job.StartValidation();
        IReadOnlyList<ParsedRow> parsedRows = parser.Parse(fileContent, entityType);

        IEnumerable<ImportRowResult> rowResults = parsedRows.Select(r =>
            r.IsValid
                ? new ImportRowResult(r.RowNumber, IsSuccess: true)
                : new ImportRowResult(r.RowNumber, IsSuccess: false,
                    ErrorMessage: string.Join("; ", r.Errors)));

        job.CompleteValidation(rowResults);
        await db.SaveChangesAsync(ct);

        return Results.Ok(new ImportJobCreatedResponse(
            job.Id,
            job.Status.ToString(),
            job.TotalRows,
            job.ErrorRows));
    }
}
