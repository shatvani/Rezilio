namespace Rezilio.Modules.Organization.Application.Queries.GetBusinessProcessById;

public sealed class GetBusinessProcessByIdHandler
{
    private readonly OrganizationDbContext _db;
    private readonly ITenantContext _tenantContext;

    public GetBusinessProcessByIdHandler(OrganizationDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [WolverineGet("/api/organization/business-processes/{id}")]
    [Authorize]
    public async Task<IResult> Handle([FromRoute] Guid id, CancellationToken ct)
    {
        BusinessProcessDto? dto = await _db.BusinessProcesses
            .AsNoTracking()
            .Where(b => b.Id == id && b.TenantId == _tenantContext.TenantId)
            .Select(b => new BusinessProcessDto(
                b.Id, b.TenantId, b.Code, b.Name, b.Category, b.CriticalityLevel,
                b.OwnerId, b.OrgUnitId, b.MaxTolerableDowntimeMinutes,
                b.RecoveryTimeObjectiveMinutes, b.DependsOnSystemIds))
            .FirstOrDefaultAsync(ct);

        if (dto is null)
        {
            return Results.NotFound("Business process not found.");
        }

        return Results.Ok(dto);
    }
}
