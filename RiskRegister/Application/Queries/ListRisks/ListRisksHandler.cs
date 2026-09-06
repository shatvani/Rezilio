namespace Rezilio.Modules.RiskRegister.Application.Queries.ListRisks;

/// <summary>
/// A fő regiszter-lista (ld. §3.1). A §3.5-ben említett, Viewer/Executive/Auditor-specifikus
/// szervezeti-hovatartozás szerinti szűrés (KeyPerson OrgUnit/BusinessProcess alapján) egy
/// későbbi finomítás — jelen v1-ben minden bejelentkezett felhasználó a teljes tenant-
/// regisztert látja, ha a lekérdezéshez van jogosultsága.
/// </summary>
public sealed class ListRisksHandler(RiskRegisterDbContext db, ITenantContext tenantContext)
{
    private const int DefaultPageSize = 25;
    private const int MaxPageSize = 200;

    [WolverineGet("/api/riskregister/risks")]
    [Authorize]
    public async Task<IResult> Handle(
        [FromQuery] RiskDomain? domain,
        [FromQuery] RiskStatus? status,
        [FromQuery] Guid? ownerId,
        [FromQuery] string? searchText,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken ct)
    {
        var effectivePage = page is > 0 ? page.Value : 1;
        var effectivePageSize = pageSize is > 0 ? Math.Min(pageSize.Value, MaxPageSize) : DefaultPageSize;

        var query = db.Risks
            .AsNoTracking()
            .Where(r => r.TenantId == tenantContext.TenantId);

        if (domain is not null)
        {
            query = query.Where(r => r.Domain == domain);
        }

        if (status is not null)
        {
            query = query.Where(r => r.Status == status);
        }

        if (ownerId is not null)
        {
            query = query.Where(r => r.OwnerId == ownerId);
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var pattern = $"%{searchText.Trim()}%";
            query = query.Where(r => EF.Functions.ILike(r.Title, pattern) || EF.Functions.ILike(r.Code, pattern));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((effectivePage - 1) * effectivePageSize)
            .Take(effectivePageSize)
            .Select(r => new RiskListItemDto(r.Id, r.Code, r.Domain, r.Title, r.OwnerId, r.Status))
            .ToListAsync(ct);

        return Results.Ok(new PagedRiskListResult(items, totalCount, effectivePage, effectivePageSize));
    }
}
