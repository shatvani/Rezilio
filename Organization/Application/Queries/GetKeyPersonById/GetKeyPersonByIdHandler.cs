using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rezilio.Modules.Organization.Infrastructure;
using Wolverine.Http;

namespace Rezilio.Modules.Organization.Application.Queries.GetKeyPersonById;

public sealed class GetKeyPersonByIdHandler(OrganizationDbContext db)
{
    [WolverineGet("/api/organization/key-persons/{id}")]
    [Authorize]
    public async Task<IResult> Handle([FromRoute] Guid id, CancellationToken ct)
    {
        var keyPerson = await db.KeyPersons
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == id, ct);

        if (keyPerson is null)
        {
            return Results.NotFound($"KeyPerson {id} nem található.");
        }

        return Results.Ok(new
        {
            keyPerson.Id,
            keyPerson.TenantId,
            keyPerson.Name,
            keyPerson.Title,
            keyPerson.Department,
            keyPerson.OrgUnitId,
            keyPerson.Email,
            keyPerson.Phone,
            keyPerson.BackupPersonName,
            keyPerson.Description
        });
    }
}
