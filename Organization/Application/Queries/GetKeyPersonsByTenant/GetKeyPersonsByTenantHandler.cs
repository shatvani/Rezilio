namespace Rezilio.Modules.Organization.Application.Queries.GetKeyPersonsByTenant;

public sealed class GetKeyPersonsByTenantHandler(OrganizationDbContext db, ITenantContext tenantContext)
{
    [WolverineGet("/api/organization/key-persons")]
    [Authorize]
    public async Task<IResult> Handle(CancellationToken ct)
    {
        var keyPersons = await db.KeyPersons
            .AsNoTracking()
            .Where(k => k.TenantId == tenantContext.TenantId)
            .OrderBy(k => k.Name)
            .Select(k => new
            {
                k.Id,
                k.Name,
                k.Code,
                k.Title,
                k.Department,
                k.OrgUnitId,
                k.Email,
                k.Phone,
                k.BackupPersonName,
                k.Description
            })
            .ToListAsync(ct);

        return Results.Ok(keyPersons);
    }
}
