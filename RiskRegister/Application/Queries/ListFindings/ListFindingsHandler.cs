namespace Rezilio.Modules.RiskRegister.Application.Queries.ListFindings;

public sealed class ListFindingsHandler(RiskRegisterDbContext db, ITenantContext tenantContext)
{
    private const int DefaultPageSize = 25;
    private const int MaxPageSize = 200;

    [WolverineGet("/api/riskregister/findings")]
    [Authorize]
    public async Task<IResult> Handle(
        [FromQuery] FindingStatus? status,
        [FromQuery] FindingSource? source,
        [FromQuery] FindingSeverity? severity,
        [FromQuery] Guid? ownerId,
        [FromQuery] string? searchText,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken ct)
    {
        var effectivePage = page is > 0 ? page.Value : 1;
        var effectivePageSize = pageSize is > 0 ? Math.Min(pageSize.Value, MaxPageSize) : DefaultPageSize;

        var query = db.Findings
            .AsNoTracking()
            .Where(f => f.TenantId == tenantContext.TenantId);

        if (status is not null)
        {
            query = query.Where(f => f.Status == status);
        }

        if (source is not null)
        {
            query = query.Where(f => f.Source == source);
        }

        if (severity is not null)
        {
            query = query.Where(f => f.Severity == severity);
        }

        if (ownerId is not null)
        {
            query = query.Where(f => f.OwnerId == ownerId);
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var pattern = $"%{searchText.Trim()}%";
            query = query.Where(f => EF.Functions.ILike(f.Description, pattern));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((effectivePage - 1) * effectivePageSize)
            .Take(effectivePageSize)
            .Select(f => new FindingListItemDto(f.Id, f.Source, f.Description, f.Severity, f.Status, f.OwnerId))
            .ToListAsync(ct);

        return Results.Ok(new PagedFindingListResult(items, totalCount, effectivePage, effectivePageSize));
    }
}
