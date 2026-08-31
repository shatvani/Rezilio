namespace Rezilio.Modules.Organization.Application.Commands.DeleteItSystem;

public sealed class DeleteItSystemHandler
{
    private readonly OrganizationDbContext _db;
    private readonly ITenantContext _tenantContext;

    public DeleteItSystemHandler(OrganizationDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [WolverineDelete("/api/organization/it-systems/{id}")]
    [Authorize]
    public async Task<IResult> Handle([FromRoute] Guid id, CancellationToken ct)
    {
        ItSystem? system = await _db.ItSystems
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == _tenantContext.TenantId, ct);
        if (system is null)
        {
            return Results.NotFound();
        }

        _db.ItSystems.Remove(system);
        await _db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
