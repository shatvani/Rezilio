namespace Rezilio.Modules.Organization.Application.Commands.DeleteBusinessProcess;

public sealed class DeleteBusinessProcessHandler
{
    private readonly OrganizationDbContext _db;
    private readonly ITenantContext _tenantContext;

    public DeleteBusinessProcessHandler(OrganizationDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [WolverineDelete("/api/organization/business-processes/{id}")]
    [Authorize]
    public async Task<IResult> Handle([FromRoute] Guid id, CancellationToken ct)
    {
        BusinessProcess? bp = await _db.BusinessProcesses
            .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == _tenantContext.TenantId, ct);
        if (bp is null)
        {
            return Results.NotFound();
        }

        _db.BusinessProcesses.Remove(bp);
        await _db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
