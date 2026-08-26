using Organization.Domain;
using Rezilio.Modules.Organization.Infrastructure;
using Rezilio.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Organization.Application.Commands.UpdateItSystem;

public sealed class UpdateItSystemHandler
{
    private readonly OrganizationDbContext _db;

    public UpdateItSystemHandler(OrganizationDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(UpdateItSystemCommand command, CancellationToken ct)
    {
        ItSystem? system = await _db.ItSystems
            .FirstOrDefaultAsync(s => s.Id == command.Id && s.TenantId == command.TenantId, ct);

        if (system is null)
        {
            return Result.Failure("IT system not found.");
        }

        bool codeExists = await _db.ItSystems
            .AnyAsync(s => s.TenantId == command.TenantId
                        && s.Code == command.Code.Trim().ToUpperInvariant()
                        && s.Id != command.Id, ct);

        if (codeExists)
        {
            return Result.Failure($"IT system with code '{command.Code}' already exists.");
        }

        system.Update(
            command.Code,
            command.Name,
            command.Type,
            command.HostingType,
            command.CriticalityLevel,
            command.Vendor,
            command.Version,
            command.OwnerId,
            command.SupportedOrgUnitIds);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
