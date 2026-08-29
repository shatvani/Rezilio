namespace Rezilio.Modules.Organization.Application.Commands.UpdateItSystem;

public sealed class UpdateItSystemHandler
{
    private readonly OrganizationDbContext _db;

    public UpdateItSystemHandler(OrganizationDbContext db)
    {
        _db = db;
    }

    [WolverinePut("/api/organization/it-systems/{id}")]
    [Authorize]
    public async Task<IResult> Handle([FromRoute] Guid id, UpdateItSystemCommand command, CancellationToken ct)
    {
        ItSystem? system = await _db.ItSystems
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == command.TenantId, ct);

        if (system is null)
        {
            return Results.NotFound("IT system not found.");
        }

        bool codeExists = await _db.ItSystems
            .AnyAsync(s => s.TenantId == command.TenantId
                        && s.Code == command.Code.Trim().ToUpperInvariant()
                        && s.Id != id, ct);

        if (codeExists)
        {
            return Results.Conflict($"IT system with code '{command.Code}' already exists.");
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
        return Results.NoContent();
    }
}
