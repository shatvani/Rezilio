using Organization.Domain;
using Rezilio.Modules.Organization.Infrastructure;
using Rezilio.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Organization.Application.Commands.CreateItSystem;

public sealed class CreateItSystemHandler
{
    private readonly OrganizationDbContext _db;

    public CreateItSystemHandler(OrganizationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(CreateItSystemCommand command, CancellationToken ct)
    {
        bool codeExists = await _db.ItSystems
            .AnyAsync(s => s.TenantId == command.TenantId && s.Code.Equals(command.Code.Trim(), StringComparison.InvariantCultureIgnoreCase), ct);

        if (codeExists)
        {
            return Result.Failure<Guid>($"IT system with code '{command.Code}' already exists.");
        }

        ItSystem system = ItSystem.Create(
            command.TenantId,
            command.Code,
            command.Name,
            command.Type,
            command.HostingType,
            command.CriticalityLevel,
            command.Vendor,
            command.Version,
            command.OwnerId,
            command.SupportedOrgUnitIds);

        _db.ItSystems.Add(system);
        await _db.SaveChangesAsync(ct);

        return Result<Guid>.Success(system.Id);
    }
}
