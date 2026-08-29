namespace Rezilio.Modules.Organization.Application.Commands.CreateItSystem;

public sealed class CreateItSystemHandler
{
    private readonly OrganizationDbContext _db;

    public CreateItSystemHandler(OrganizationDbContext db)
    {
        _db = db;
    }

    [WolverinePost("/api/organization/it-systems")]
    [Authorize]
    public async Task<IResult> Handle(CreateItSystemCommand command, CancellationToken ct)
    {
        bool codeExists = await _db.ItSystems
            .AnyAsync(s => s.TenantId == command.TenantId
                        && s.Code.Equals(command.Code.Trim(), StringComparison.InvariantCultureIgnoreCase), ct);

        if (codeExists)
        {
            return Results.Conflict($"IT system with code '{command.Code}' already exists.");
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

        return Results.Created($"/api/organization/it-systems/{system.Id}", new { system.Id });
    }
}
