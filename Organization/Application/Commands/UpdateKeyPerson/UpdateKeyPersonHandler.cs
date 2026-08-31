namespace Rezilio.Modules.Organization.Application.Commands.UpdateKeyPerson;

public sealed class UpdateKeyPersonHandler(OrganizationDbContext db, ITenantContext tenantContext)
{
    [WolverinePut("/api/organization/key-persons/{id}")]
    [Authorize]
    public async Task<IResult> Handle([FromRoute] Guid id, UpdateKeyPersonCommand command, CancellationToken ct)
    {
        var keyPerson = await db.KeyPersons
            .FirstOrDefaultAsync(k => k.Id == id && k.TenantId == tenantContext.TenantId, ct);
        if (keyPerson is null)
        {
            return Results.NotFound($"KeyPerson {id} nem található.");
        }

        bool codeExists = await db.KeyPersons
            .AnyAsync(k => k.TenantId == tenantContext.TenantId
                        && k.Code == command.Code.Trim().ToUpperInvariant()
                        && k.Id != id, ct);

        if (codeExists)
        {
            return Results.Conflict($"Key person with code '{command.Code}' already exists.");
        }

        keyPerson.Update(
            command.Name,
            command.Code,
            command.Title,
            command.Department,
            command.OrgUnitId,
            command.Email,
            command.Phone,
            command.BackupPersonName,
            command.Description);

        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
