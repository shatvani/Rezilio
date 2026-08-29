using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Organization.Domain;
using Rezilio.Modules.Organization.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace Organization.Application.Commands.UpdateBusinessProcess;

public sealed class UpdateBusinessProcessHandler
{
    private readonly OrganizationDbContext _db;

    public UpdateBusinessProcessHandler(OrganizationDbContext db)
    {
        _db = db;
    }

    [WolverinePut("/api/organization/business-processes/{id}")]
    [Authorize]
    public async Task<IResult> Handle([FromRoute] Guid id, UpdateBusinessProcessCommand command, CancellationToken ct)
    {
        BusinessProcess? bp = await _db.BusinessProcesses
            .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == command.TenantId, ct);

        if (bp is null)
        {
            return Results.NotFound("Business process not found.");
        }

        bool codeExists = await _db.BusinessProcesses
            .AnyAsync(b => b.TenantId == command.TenantId
                        && b.Code == command.Code.Trim().ToUpperInvariant()
                        && b.Id != id, ct);

        if (codeExists)
        {
            return Results.Conflict($"Business process with code '{command.Code}' already exists.");
        }

        var updateResult = bp.Update(
            command.Code,
            command.Name,
            command.Category,
            command.CriticalityLevel,
            command.OwnerId,
            command.OrgUnitId,
            command.MaxTolerableDowntimeMinutes,
            command.RecoveryTimeObjectiveMinutes,
            command.DependsOnSystemIds);

        if (updateResult.IsFailure)
        {
            return Results.BadRequest(updateResult.Error);
        }

        await _db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
