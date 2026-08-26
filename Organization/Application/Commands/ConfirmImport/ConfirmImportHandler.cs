using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Organization.Domain;
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
        else if (job.EntityType == EntityType.KeyPerson)
        {
            await ImportKeyPersonsAsync(job, ct);
        }
        else if (job.EntityType == EntityType.ItSystem)
        {
            await ImportItSystemsAsync(job, ct);
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

    private async Task ImportKeyPersonsAsync(ImportJob job, CancellationToken ct)
    {
        IReadOnlyList<ParsedRow> rows = parser.Parse(job.FileContent, job.EntityType);
        var orgUnitsByCode = await db.OrganizationalUnits
            .Where(u => u.TenantId == job.TenantId)
            .ToDictionaryAsync(u => u.Code, u => u.Id, ct);

        foreach (ParsedRow row in rows.Where(r => r.IsValid))
        {
            string name = row.Values["Name"]!;
            row.Values.TryGetValue("Title", out string? title);
            row.Values.TryGetValue("Department", out string? department);
            row.Values.TryGetValue("OrgUnitCode", out string? orgUnitCode);
            row.Values.TryGetValue("Email", out string? email);
            row.Values.TryGetValue("Phone", out string? phone);
            row.Values.TryGetValue("BackupPersonName", out string? backupPersonName);
            row.Values.TryGetValue("Description", out string? description);

            Guid? orgUnitId = null;
            if (!string.IsNullOrWhiteSpace(orgUnitCode))
            {
                string normalized = orgUnitCode.Trim().ToUpperInvariant();
                if (orgUnitsByCode.TryGetValue(normalized, out Guid uid))
                {
                    orgUnitId = uid;
                }
            }

            var keyPerson = KeyPerson.Create(
                job.TenantId, name, title, department,
                orgUnitId, email, phone, backupPersonName, description);

            db.KeyPersons.Add(keyPerson);
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task ImportItSystemsAsync(ImportJob job, CancellationToken ct)
    {
        IReadOnlyList<ParsedRow> rows = parser.Parse(job.FileContent, job.EntityType);

        Dictionary<string, Guid> orgUnitsByCode = await db.OrganizationalUnits
            .Where(u => u.TenantId == job.TenantId)
            .ToDictionaryAsync(u => u.Code, u => u.Id, ct);

        Dictionary<string, Guid> keyPersonsByCode = await db.KeyPersons
            .Where(k => k.TenantId == job.TenantId)
            .ToDictionaryAsync(k => k.Name, k => k.Id, ct);

        // OwnerId lookup: OwnerCode oszlop → KeyPerson.Name alapú keresés
        // Ha pontosabb matching kell, KeyPersonnek is kellene Code mező

        foreach (ParsedRow row in rows)
        {
            string code = (row.Values.GetValueOrDefault("Code") ?? string.Empty).Trim().ToUpperInvariant();

            List<Guid> supportedOrgUnitIds = [];
            string orgUnitCodesRaw = row.Values.GetValueOrDefault("SupportedOrgUnitCodes", "") ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(orgUnitCodesRaw))
            {
                foreach (string part in orgUnitCodesRaw.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    string orgUnitCode = part.Trim().ToUpperInvariant();
                    if (orgUnitsByCode.TryGetValue(orgUnitCode, out Guid orgUnitId))
                    {
                        supportedOrgUnitIds.Add(orgUnitId);
                    }
                }
            }

            Guid? ownerId = null;
            string ownerCode = (row.Values.GetValueOrDefault("OwnerCode") ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(ownerCode) && keyPersonsByCode.TryGetValue(ownerCode, out Guid kpId))
            {
                ownerId = kpId;
            }

            if (!Enum.TryParse<ItSystemType>(row.Values.GetValueOrDefault("Type"), true, out ItSystemType type))
            {
                type = ItSystemType.Other;
            }

            if (!Enum.TryParse<HostingType>(row.Values.GetValueOrDefault("HostingType"), true, out HostingType hostingType))
            {
                hostingType = HostingType.OnPrem;
            }

            if (!Enum.TryParse<CriticalityLevel>(row.Values.GetValueOrDefault("CriticalityLevel"), true, out CriticalityLevel criticalityLevel))
            {
                criticalityLevel = CriticalityLevel.Low;
            }

            ItSystem? existing = await db.ItSystems
                .FirstOrDefaultAsync(s => s.TenantId == job.TenantId && s.Code == code, ct);

            if (existing is null)
            {
                ItSystem system = ItSystem.Create(
                    job.TenantId, code,
                    row.Values.GetValueOrDefault("Name", "") ?? string.Empty,
                    type, hostingType, criticalityLevel,
                    row.Values.GetValueOrDefault("Vendor"),
                    row.Values.GetValueOrDefault("Version"),
                    ownerId, supportedOrgUnitIds);
                db.ItSystems.Add(system);
            }
            else
            {
                existing.Update(
                    code,
                    row.Values.GetValueOrDefault("Name", "") ?? string.Empty,
                    type, hostingType, criticalityLevel,
                    row.Values.GetValueOrDefault("Vendor"),
                    row.Values.GetValueOrDefault("Version"),
                    ownerId, supportedOrgUnitIds);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
