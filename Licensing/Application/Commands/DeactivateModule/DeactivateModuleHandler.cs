namespace Rezilio.Modules.Licensing.Application.Commands.DeactivateModule;

public sealed class DeactivateModuleHandler(LicensingDbContext db)
{
    [WolverinePost("/api/licensing/modules/{tenantId}/{module}/deactivate")]
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

        license.DeactivateModule(module);
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
