using Organization.Domain;
using Rezilio.Modules.Organization.Infrastructure;
using Rezilio.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Organization.Application.Commands.UpdateBusinessProcess;

public sealed class UpdateBusinessProcessHandler
{
    private readonly OrganizationDbContext _db;

    public UpdateBusinessProcessHandler(OrganizationDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(UpdateBusinessProcessCommand command, CancellationToken ct)
    {
        BusinessProcess? bp = await _db.BusinessProcesses
            .FirstOrDefaultAsync(b => b.Id == command.Id && b.TenantId == command.TenantId, ct);

        if (bp is null)
        {
            return Result.Failure("Business process not found.");
        }

        bool codeExists = await _db.BusinessProcesses
            .AnyAsync(b => b.TenantId == command.TenantId
                        && b.Code == command.Code.Trim().ToUpperInvariant()
                        && b.Id != command.Id, ct);

        if (codeExists)
        {
            return Result.Failure($"Business process with code '{command.Code}' already exists.");
        }

        Result updateResult = bp.Update(
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
            return updateResult;
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
