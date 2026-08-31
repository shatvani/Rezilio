namespace Rezilio.Modules.Licensing.Application.Commands.ActivateModule;

public sealed class ActivateModuleHandler(LicensingDbContext db, ITenantContext tenantContext)
{
    [WolverinePost("/api/licensing/modules/{tenantId}/{module}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<IResult> Handle(
        [FromRoute] Guid tenantId,
        [FromRoute] ModuleType module,
        CancellationToken ct)
    {
        TenantLicense? license = await db.Licenses
            .FirstOrDefaultAsync(l => l.TenantId == tenantContext.TenantId, ct);

        if (license is null)
        {
            return Results.NotFound($"Nincs licensz a(z) {tenantContext.TenantId} tenanthoz.");
        }

        license.ActivateModule(module);
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
