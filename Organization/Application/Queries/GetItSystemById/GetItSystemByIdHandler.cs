using Rezilio.Modules.Organization.Infrastructure;
using Rezilio.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Organization.Application.Queries.GetItSystemById;

public sealed class GetItSystemByIdHandler
{
    private readonly OrganizationDbContext _db;

    public GetItSystemByIdHandler(OrganizationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<ItSystemDto>> Handle(GetItSystemByIdQuery query, CancellationToken ct)
    {
        ItSystemDto? dto = await _db.ItSystems
            .Where(s => s.Id == query.Id && s.TenantId == query.TenantId)
            .Select(s => new ItSystemDto(
                s.Id, s.TenantId, s.Code, s.Name, s.Type, s.HostingType,
                s.CriticalityLevel, s.Vendor, s.Version, s.OwnerId, s.SupportedOrgUnitIds))
            .FirstOrDefaultAsync(ct);

        if (dto is null)
        {
            return Result.Failure<ItSystemDto>("IT system not found.");
        }

        return Result<ItSystemDto>.Success(dto);
    }
}
