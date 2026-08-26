using Organization.Application.Queries.GetItSystemById;
using Rezilio.Modules.Organization.Infrastructure;
using Rezilio.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Organization.Application.Queries.GetItSystemsByTenant;

public sealed class GetItSystemsByTenantHandler
{
    private readonly OrganizationDbContext _db;

    public GetItSystemsByTenantHandler(OrganizationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<ItSystemDto>>> Handle(GetItSystemsByTenantQuery query, CancellationToken ct)
    {
        List<ItSystemDto> systems = await _db.ItSystems
            .Where(s => s.TenantId == query.TenantId)
            .OrderBy(s => s.Code)
            .Select(s => new ItSystemDto(
                s.Id, s.TenantId, s.Code, s.Name, s.Type, s.HostingType,
                s.CriticalityLevel, s.Vendor, s.Version, s.OwnerId, s.SupportedOrgUnitIds))
            .ToListAsync(ct);

        return Result<List<ItSystemDto>>.Success(systems);
    }
}
