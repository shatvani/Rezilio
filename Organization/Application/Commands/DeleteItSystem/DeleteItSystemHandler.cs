using Organization.Domain;
using Rezilio.Modules.Organization.Infrastructure;
using Rezilio.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Organization.Application.Commands.DeleteItSystem;

public sealed record DeleteItSystemCommand(Guid Id, Guid TenantId);

public sealed class DeleteItSystemHandler
{
    private readonly OrganizationDbContext _db;

    public DeleteItSystemHandler(OrganizationDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(DeleteItSystemCommand command, CancellationToken ct)
    {
        ItSystem? system = await _db.ItSystems
            .FirstOrDefaultAsync(s => s.Id == command.Id && s.TenantId == command.TenantId, ct);

        if (system is null)
        {
            return Result.Failure("IT system not found.");
        }

        _db.ItSystems.Remove(system);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
