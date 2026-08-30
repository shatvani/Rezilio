namespace Rezilio.Modules.Licensing.Application.Commands.StartTrial;

public sealed class StartTrialHandler(LicensingDbContext db)
{
    [WolverinePost("/api/licensing/modules/{tenantId}/{module}/start-trial")]
    [Authorize(Roles = "Admin")]
    public async Task<IResult> Handle(
        [FromRoute] Guid tenantId,
        [FromRoute] ModuleType module,
        CancellationToken ct)
    {
        TenantLicense? license = await db.Licenses
            .FirstOrDefaultAsync(l => l.TenantId == tenantId, ct);

        if (license is null)
        {
            return Results.NotFound($"Nincs licensz a(z) {tenantId} tenanthoz.");
        }

        license.StartTrial(module);
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
