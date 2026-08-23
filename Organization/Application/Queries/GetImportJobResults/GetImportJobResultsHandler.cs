using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rezilio.Modules.Organization.Domain;
using Rezilio.Modules.Organization.Infrastructure;
using Wolverine.Http;

namespace Rezilio.Modules.Organization.Application.Queries.GetImportJobResults;

public sealed record ImportRowResultResponse(
    int RowNumber,
    bool IsSuccess,
    string? ErrorMessage,
    string? ColumnName);

public sealed class GetImportJobResultsHandler(OrganizationDbContext db)
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
            .FirstOrDefaultAsync(j => j.Id == importJobId, ct);

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
