using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rezilio.Modules.Organization.Domain;
using Rezilio.Modules.Organization.Infrastructure;
using Wolverine.Http;

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

public sealed class GetImportJobStatusHandler(OrganizationDbContext db)
{
    [WolverineGet("/api/organization/import/{importJobId}/status")]
    [Authorize]
    public async Task<IResult> Handle(
        [FromRoute] Guid importJobId,
        CancellationToken ct)
    {
        ImportJob? job = await db.ImportJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == importJobId, ct);

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
