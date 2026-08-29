namespace Rezilio.Modules.Organization.Application.Commands.UpdateKeyPerson;

public sealed class UpdateKeyPersonHandler(OrganizationDbContext db)
{
    [WolverinePut("/api/organization/key-persons/{id}")]
    [Authorize]
    public async Task<IResult> Handle([FromRoute] Guid id, UpdateKeyPersonCommand command, CancellationToken ct)
    {
        var keyPerson = await db.KeyPersons.FirstOrDefaultAsync(k => k.Id == id, ct);
        if (keyPerson is null)
        {
            return Results.NotFound($"KeyPerson {id} nem található.");
        }

        keyPerson.Update(
            command.Name,
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
