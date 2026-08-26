using Organization.Domain;
using Rezilio.Modules.Organization.Infrastructure;
using Rezilio.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Organization.Application.Commands.DeleteBusinessProcess;

public sealed record DeleteBusinessProcessCommand(Guid Id, Guid TenantId);

public sealed class DeleteBusinessProcessHandler
{
    private readonly OrganizationDbContext _db;

    public DeleteBusinessProcessHandler(OrganizationDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(DeleteBusinessProcessCommand command, CancellationToken ct)
    {
        BusinessProcess? bp = await _db.BusinessProcesses
            .FirstOrDefaultAsync(b => b.Id == command.Id && b.TenantId == command.TenantId, ct);

        if (bp is null)
        {
            return Result.Failure("Business process not found.");
        }

        _db.BusinessProcesses.Remove(bp);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
