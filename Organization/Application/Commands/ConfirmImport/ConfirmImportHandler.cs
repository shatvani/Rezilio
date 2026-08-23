using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rezilio.Modules.Organization.Domain;
using Rezilio.Modules.Organization.Infrastructure;
using Wolverine.Http;

namespace Rezilio.Modules.Organization.Application.Commands.ConfirmImport;

public sealed class ConfirmImportHandler(OrganizationDbContext db)
{
    [WolverinePost("/api/organization/import/{importJobId}/confirm")]
    [Authorize]
    public async Task<IResult> Handle(
        [FromRoute] Guid importJobId,
        CancellationToken ct)
    {
        ImportJob? job = await db.ImportJobs
            .FirstOrDefaultAsync(j => j.Id == importJobId, ct);

        if (job is null)
        {
            return Results.NotFound($"ImportJob {importJobId} nem található.");
        }

        job.StartImport();
        await db.SaveChangesAsync(ct);

        try
        {
            // TODO (ORG.3–ORG.9): EntityType-specifikus import logika ide kerül
            // pl.: await _importDispatcher.ImportAsync(job, ct);

            job.Complete();
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { job.Id, Status = job.Status.ToString(), job.SuccessRows });
        }
        catch (Exception ex)
        {
            job.Fail(ex.Message);
            await db.SaveChangesAsync(ct);
            return Results.Problem($"Import meghiúsult: {ex.Message}");
        }
    }
}
