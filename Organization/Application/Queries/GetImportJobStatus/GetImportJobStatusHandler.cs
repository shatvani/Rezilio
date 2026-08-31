namespace Rezilio.Modules.Organization.Application.Queries.GetImportJobStatus;

public sealed record ImportJobStatusResponse(
    Guid Id,
    string EntityType,
    string Status,
    int TotalRows,
    int SuccessRows,
    int ErrorRows,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

public sealed class GetImportJobStatusHandler(OrganizationDbContext db, ITenantContext tenantContext)
{
    [WolverineGet("/api/organization/import/{importJobId}/status")]
    [Authorize]
    public async Task<IResult> Handle(
        [FromRoute] Guid importJobId,
        CancellationToken ct)
    {
        ImportJob? job = await db.ImportJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == importJobId && j.TenantId == tenantContext.TenantId, ct);

        if (job is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new ImportJobStatusResponse(
            job.Id,
            job.EntityType.ToString(),
            job.Status.ToString(),
            job.TotalRows,
            job.SuccessRows,
            job.ErrorRows,
            job.CreatedAt,
            job.CompletedAt));
    }
}
