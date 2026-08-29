using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Organization.Domain;
using Rezilio.Modules.Organization.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace Organization.Application.Commands.DeleteBusinessProcess;

public sealed class DeleteBusinessProcessHandler
{
    private readonly OrganizationDbContext _db;

    public DeleteBusinessProcessHandler(OrganizationDbContext db)
    {
        _db = db;
    }

    [WolverineDelete("/api/organization/business-processes/{id}")]
    [Authorize]
    public async Task<IResult> Handle([FromRoute] Guid id, CancellationToken ct)
    {
        BusinessProcess? bp = await _db.BusinessProcesses.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (bp is null)
        {
            return Results.NotFound();
        }

        _db.BusinessProcesses.Remove(bp);
        await _db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
