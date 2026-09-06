using Rezilio.SharedKernel.Contracts.Organization;

namespace Rezilio.Modules.Organization.Application.Queries.KeyPersonLookup;

/// <summary>
/// Válaszol a <see cref="KeyPersonLookupQuery"/>-re — modulközi, szinkron Wolverine-üzenet
/// (nincs HTTP végpontja, csak <c>IMessageBus.InvokeAsync</c>-kal hívható más modulokból,
/// pl. RiskRegister). Lásd docs/design/risk-register-design.md §2.5.
/// </summary>
public static class KeyPersonLookupHandler
{
    public static async Task<KeyPersonLookupResult> Handle(
        KeyPersonLookupQuery query,
        OrganizationDbContext db,
        CancellationToken ct)
    {
        var keyPerson = await db.KeyPersons
            .AsNoTracking()
            .Where(k => k.Id == query.KeyPersonId && k.TenantId == query.TenantId)
            .Select(k => new { k.IsActive })
            .FirstOrDefaultAsync(ct);

        if (keyPerson is null)
        {
            return new KeyPersonLookupResult(Exists: false, IsActive: false);
        }

        return new KeyPersonLookupResult(Exists: true, keyPerson.IsActive);
    }
}
