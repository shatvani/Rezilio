namespace Rezilio.Modules.Organization.Application.Queries.GetImportJobResults;

public sealed record ImportRowResultResponse(
    int RowNumber,
    bool IsSuccess,
    string? ErrorMessage,
    string? ColumnName);

public sealed class GetImportJobResultsHandler(OrganizationDbContext db, ITenantContext tenantContext)
{
    [WolverineGet("/api/organization/import/{importJobId}/results")]
    [Authorize]
    public async Task<IResult> Handle(
        [FromRoute] Guid importJobId,
        [FromQuery] bool? errorsOnly,
        CancellationToken ct)
    {
        ImportJob? job = await db.ImportJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == importJobId && j.TenantId == tenantContext.TenantId, ct);

        if (job is null)
        {
            return Results.NotFound();
        }

        IEnumerable<ImportRowResult> results = errorsOnly == true
            ? job.Results.Where(r => !r.IsSuccess)
            : job.Results;

        return Results.Ok(results.Select(r => new ImportRowResultResponse(
            r.RowNumber,
            r.IsSuccess,
            r.ErrorMessage,
            r.ColumnName)));
    }
}
