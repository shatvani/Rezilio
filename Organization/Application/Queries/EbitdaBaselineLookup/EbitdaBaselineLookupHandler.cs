using Rezilio.SharedKernel.Contracts.Organization;

namespace Rezilio.Modules.Organization.Application.Queries.EbitdaBaselineLookup;

/// <summary>
/// Válaszol az <see cref="EbitdaBaselineLookupQuery"/>-re — modulközi, szinkron
/// Wolverine-üzenet (nincs HTTP végpontja, csak <c>IMessageBus.InvokeAsync</c>-kal hívható
/// más modulokból, pl. RiskRegister). Implementálja a teljes EBITDA-alapérték prioritás-
/// logikát: OrgUnit/BusinessProcess-szintű felülírás &gt; tenant-szintű alapérték &gt; null.
/// Ld. docs/design/risk-register-design.md §2.3/§3.3.
/// </summary>
public static class EbitdaBaselineLookupHandler
{
    public static async Task<EbitdaBaselineLookupResult> Handle(
        EbitdaBaselineLookupQuery query,
        OrganizationDbContext db,
        CancellationToken ct)
    {
        // 1. Kontextus-szintű (OrgUnit vagy BusinessProcess) felülírás — elsőbbséget élvez.
        if (query.OrgUnitId is { } orgUnitId)
        {
            var orgUnitEbitda = await db.OrganizationalUnits
                .AsNoTracking()
                .Where(o => o.Id == orgUnitId && o.TenantId == query.TenantId)
                .Select(o => o.AnnualEbitda)
                .FirstOrDefaultAsync(ct);

            if (orgUnitEbitda is not null)
            {
                return new EbitdaBaselineLookupResult(orgUnitEbitda);
            }
        }
        else if (query.BusinessProcessId is { } businessProcessId)
        {
            var businessProcessEbitda = await db.BusinessProcesses
                .AsNoTracking()
                .Where(b => b.Id == businessProcessId && b.TenantId == query.TenantId)
                .Select(b => b.AnnualEbitda)
                .FirstOrDefaultAsync(ct);

            if (businessProcessEbitda is not null)
            {
                return new EbitdaBaselineLookupResult(businessProcessEbitda);
            }
        }

        // 2. Nincs kontextus-szintű felülírás (vagy nincs is megadva kontextus) — tenant-szintű
        //    alapértékre esünk vissza. Ha az sincs beállítva, a null jut vissza (nem hibaállapot).
        var tenantDefault = await db.TenantSettings
            .AsNoTracking()
            .Where(t => t.TenantId == query.TenantId)
            .Select(t => t.DefaultAnnualEbitda)
            .FirstOrDefaultAsync(ct);

        return new EbitdaBaselineLookupResult(tenantDefault);
    }
}
