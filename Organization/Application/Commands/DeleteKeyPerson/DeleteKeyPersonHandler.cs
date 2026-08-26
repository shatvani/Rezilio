using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rezilio.Modules.Organization.Infrastructure;
using Wolverine.Http;

namespace Rezilio.Modules.Organization.Application.Commands.DeleteKeyPerson;

public sealed class DeleteKeyPersonHandler(OrganizationDbContext db)
{
    [WolverineDelete("/api/organization/key-persons/{id}")]
    [Authorize]
    public async Task<IResult> Handle([FromRoute] Guid id, CancellationToken ct)
    {
        var keyPerson = await db.KeyPersons.FirstOrDefaultAsync(k => k.Id == id, ct);
        if (keyPerson is null)
        {
            return Results.NotFound($"KeyPerson {id} nem található.");
        }

        db.KeyPersons.Remove(keyPerson);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
