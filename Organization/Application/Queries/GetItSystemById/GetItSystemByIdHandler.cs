namespace Rezilio.Modules.Organization.Application.Queries.GetItSystemById;

public sealed class GetItSystemByIdHandler
{
    private readonly OrganizationDbContext _db;
    private readonly ITenantContext _tenantContext;

    public GetItSystemByIdHandler(OrganizationDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [WolverineGet("/api/organization/it-systems/{id}")]
    [Authorize]
    public async Task<IResult> Handle([FromRoute] Guid id, CancellationToken ct)
    {
        ItSystemDto? dto = await _db.ItSystems
            .AsNoTracking()
            .Where(s => s.Id == id && s.TenantId == _tenantContext.TenantId)
            .Select(s => new ItSystemDto(
                s.Id, s.TenantId, s.Code, s.Name, s.Type, s.HostingType,
                s.CriticalityLevel, s.Vendor, s.Version, s.OwnerId, s.SupportedOrgUnitIds))
            .FirstOrDefaultAsync(ct);

        if (dto is null)
        {
            return Results.NotFound("IT system not found.");
        }

        return Results.Ok(dto);
    }
}
