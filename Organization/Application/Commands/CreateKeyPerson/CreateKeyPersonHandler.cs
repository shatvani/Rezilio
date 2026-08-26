using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Rezilio.Modules.Organization.Domain;
using Rezilio.Modules.Organization.Infrastructure;
using Wolverine.Http;

namespace Rezilio.Modules.Organization.Application.Commands.CreateKeyPerson;

public sealed class CreateKeyPersonHandler(OrganizationDbContext db)
{
    [WolverinePost("/api/organization/key-persons")]
    [Authorize]
    public async Task<IResult> Handle(CreateKeyPersonCommand command, CancellationToken ct)
    {
        var keyPerson = KeyPerson.Create(
            command.TenantId,
            command.Name,
            command.Title,
            command.Department,
            command.OrgUnitId,
            command.Email,
            command.Phone,
            command.BackupPersonName,
            command.Description);

        db.KeyPersons.Add(keyPerson);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/organization/key-persons/{keyPerson.Id}", new { keyPerson.Id });
    }
}
