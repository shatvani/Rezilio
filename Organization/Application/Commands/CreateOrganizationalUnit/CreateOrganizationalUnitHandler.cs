using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Rezilio.Modules.Organization.Domain;
using Rezilio.Modules.Organization.Infrastructure;
using Wolverine.Http;

namespace Rezilio.Modules.Organization.Application.Commands.CreateOrganizationalUnit;

public sealed record CreateOrganizationalUnitResponse(Guid Id, string Name, string Code);

public sealed class CreateOrganizationalUnitHandler(OrganizationDbContext db)
{
    [WolverinePost("/api/organization/units")]
    [Authorize]
    public async Task<IResult> Handle(
        CreateOrganizationalUnitCommand command,
        CancellationToken ct)
    {
        bool codeExists = await db.OrganizationalUnits
            .AnyAsync(u => u.TenantId == command.TenantId
                        && u.Code == command.Code.Trim().ToUpperInvariant(), ct);

        if (codeExists)
        {
            return Results.Conflict($"Organizational unit with code '{command.Code}' already exists.");
        }

        var unit = OrganizationalUnit.Create(
            command.TenantId,
            command.Name,
            command.Code,
            command.ParentId,
            command.Description);

        db.OrganizationalUnits.Add(unit);
        await db.SaveChangesAsync(ct);

        return Results.Created(
            $"/api/organization/units/{unit.Id}",
            new CreateOrganizationalUnitResponse(unit.Id, unit.Name, unit.Code));
    }
}
