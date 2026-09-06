namespace Rezilio.Modules.Organization.Application.Commands.DeleteKeyPerson;

public sealed class DeleteKeyPersonHandler(OrganizationDbContext db, ITenantContext tenantContext)
{
    // 2026-09-06: kemény törlés helyett deaktiválás (soft-delete) — lásd KeyPerson.Deactivate()
    // indoklását. A route/HTTP verb változatlan marad (a hívó oldal API-szerződése nem változik).
    [WolverineDelete("/api/organization/key-persons/{id}")]
    [Authorize]
    public async Task<IResult> Handle([FromRoute] Guid id, CancellationToken ct)
    {
        var keyPerson = await db.KeyPersons
            .FirstOrDefaultAsync(k => k.Id == id && k.TenantId == tenantContext.TenantId, ct);
        if (keyPerson is null)
        {
            return Results.NotFound($"KeyPerson {id} nem található.");
        }

        keyPerson.Deactivate();
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
