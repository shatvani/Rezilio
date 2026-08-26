using Organization.Domain;
using Rezilio.Modules.Organization.Infrastructure;
using Rezilio.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Organization.Application.Commands.CreateBusinessProcess;

public sealed class CreateBusinessProcessHandler
{
    private readonly OrganizationDbContext _db;

    public CreateBusinessProcessHandler(OrganizationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(CreateBusinessProcessCommand command, CancellationToken ct)
    {
        bool codeExists = await _db.BusinessProcesses
            .AnyAsync(b => b.TenantId == command.TenantId
                        && b.Code == command.Code.Trim().ToUpperInvariant(), ct);

        if (codeExists)
        {
            return Result.Failure<Guid>($"Business process with code '{command.Code}' already exists.");
        }

        Result<BusinessProcess> result = BusinessProcess.Create(
            command.TenantId,
            command.Code,
            command.Name,
            command.Category,
            command.CriticalityLevel,
            command.OwnerId,
            command.OrgUnitId,
            command.MaxTolerableDowntimeMinutes,
            command.RecoveryTimeObjectiveMinutes,
            command.DependsOnSystemIds);

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error!);
        }

        _db.BusinessProcesses.Add(result.Value);
        await _db.SaveChangesAsync(ct);
        return Result.Success(result.Value.Id);
    }
}
