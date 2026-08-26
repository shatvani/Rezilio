using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rezilio.Modules.Organization.Application.Services;
using Rezilio.Modules.Organization.Domain;
using Rezilio.Modules.Organization.Infrastructure;
using Wolverine.Http;

namespace Rezilio.Modules.Organization.Application.Commands.ConfirmImport;

public sealed class ConfirmImportHandler(
    OrganizationDbContext db,
    IExcelImportParser parser)
{
    [WolverinePost("/api/organization/import/{importJobId}/confirm")]
    [Authorize]
    public async Task<IResult> Handle(
        [FromRoute] Guid importJobId,
        CancellationToken ct)
    {
        ImportJob? job = await db.ImportJobs
            .FirstOrDefaultAsync(j => j.Id == importJobId, ct);

        if (job is null)
        {
            return Results.NotFound($"ImportJob {importJobId} nem található.");
        }

        job.StartImport();
        await db.SaveChangesAsync(ct);

        try
        {
            await ImportEntitiesAsync(job, ct);
            job.Complete();
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { job.Id, Status = job.Status.ToString(), job.SuccessRows });
        }
        catch (Exception ex)
        {
            job.Fail(ex.Message);
            await db.SaveChangesAsync(ct);
            return Results.Problem($"Import meghiúsult: {ex.Message}");
        }
    }

    private async Task ImportEntitiesAsync(ImportJob job, CancellationToken ct)
    {
        if (job.EntityType == EntityType.OrganizationalUnit)
        {
            await ImportOrganizationalUnitsAsync(job, ct);
        }
        else if (job.EntityType == EntityType.Customer)
        {
            await ImportCustomersAsync(job, ct);
        }
        else if (job.EntityType == EntityType.Supplier)
        {
            await ImportSuppliersAsync(job, ct);
        }
        else
        {
            throw new NotSupportedException(
                $"EntityType '{job.EntityType}' importja még nem implementált.");
        }
    }

    private async Task ImportOrganizationalUnitsAsync(ImportJob job, CancellationToken ct)
    {
        // Fájl újraparse-olása
        IReadOnlyList<ParsedRow> rows = parser.Parse(job.FileContent, job.EntityType);

        // Meglévő egységek kód → Id map (szülő feloldáshoz)
        var existingByCode = await db.OrganizationalUnits
            .Where(u => u.TenantId == job.TenantId)
            .ToDictionaryAsync(u => u.Code, u => u.Id, ct);

        foreach (ParsedRow row in rows.Where(r => r.IsValid))
        {
            string name = row.Values["Name"]!;
            string code = row.Values["Code"]!;

            row.Values.TryGetValue("ParentCode", out string? parentCode);
            row.Values.TryGetValue("Description", out string? description);

            Guid? parentId = null;
            if (!string.IsNullOrWhiteSpace(parentCode))
            {
                string parentCodeUpper = parentCode.Trim().ToUpperInvariant();
                if (existingByCode.TryGetValue(parentCodeUpper, out Guid pid))
                {
                    parentId = pid;
                }
                // Ha a szülő nem létezik → figyelmeztetés nélkül folytatjuk (ParentCode opcionális)
            }

            var unit = OrganizationalUnit.Create(job.TenantId, name, code, parentId, description);
            db.OrganizationalUnits.Add(unit);

            // Frissen létrehozott egységet is felvesszük a mapbe
            // (ha egy import fájlon belül hierarchia van, a következő sorok megtalálják)
            existingByCode[unit.Code] = unit.Id;
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task ImportCustomersAsync(ImportJob job, CancellationToken ct)
    {
        IReadOnlyList<ParsedRow> rows = parser.Parse(job.FileContent, job.EntityType);
        var existingCodes = await db.Customers
            .Where(c => c.TenantId == job.TenantId)
            .Select(c => c.Code)
            .ToHashSetAsync(ct);

        foreach (ParsedRow row in rows.Where(r => r.IsValid))
        {
            string name = row.Values["Name"]!;
            string code = row.Values["Code"]!;
            row.Values.TryGetValue("Industry", out string? industry);
            row.Values.TryGetValue("Country", out string? country);
            row.Values.TryGetValue("ContactEmail", out string? contactEmail);
            row.Values.TryGetValue("ContactPhone", out string? contactPhone);
            row.Values.TryGetValue("Description", out string? description);

            string normalizedCode = code.Trim().ToUpperInvariant();
            if (existingCodes.Contains(normalizedCode))
            {
                continue;
            }

            var customer = Customer.Create(
                job.TenantId, name, code,
                industry, country, contactEmail, contactPhone, description);

            db.Customers.Add(customer);
            existingCodes.Add(normalizedCode);
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task ImportSuppliersAsync(ImportJob job, CancellationToken ct)
    {
        IReadOnlyList<ParsedRow> rows = parser.Parse(job.FileContent, job.EntityType);
        var existingCodes = await db.Suppliers
            .Where(s => s.TenantId == job.TenantId)
            .Select(s => s.Code)
            .ToHashSetAsync(ct);

        foreach (ParsedRow row in rows.Where(r => r.IsValid))
        {
            string name = row.Values["Name"]!;
            string code = row.Values["Code"]!;
            row.Values.TryGetValue("Industry", out string? industry);
            row.Values.TryGetValue("Country", out string? country);
            row.Values.TryGetValue("ContactEmail", out string? contactEmail);
            row.Values.TryGetValue("ContactPhone", out string? contactPhone);
            row.Values.TryGetValue("Description", out string? description);

            string normalizedCode = code.Trim().ToUpperInvariant();
            if (existingCodes.Contains(normalizedCode))
            {
                continue;
            }

            var supplier = Supplier.Create(
                job.TenantId, name, code,
                industry, country, contactEmail, contactPhone, description);

            db.Suppliers.Add(supplier);
            existingCodes.Add(normalizedCode);
        }

        await db.SaveChangesAsync(ct);
    }
}
