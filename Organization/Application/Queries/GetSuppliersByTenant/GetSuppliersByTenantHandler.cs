namespace Rezilio.Modules.Organization.Application.Queries.GetSuppliersByTenant;

public sealed class GetSuppliersByTenantHandler(OrganizationDbContext db, ITenantContext tenantContext)
{
    [WolverineGet("/api/organization/suppliers")]
    [Authorize]
    public async Task<IResult> Handle(CancellationToken ct)
    {
        var suppliers = await db.Suppliers
            .AsNoTracking()
            .Where(s => s.TenantId == tenantContext.TenantId)
            .OrderBy(s => s.Name)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Code,
                s.Industry,
                s.Country,
                s.ContactEmail,
                s.ContactPhone,
                s.Description
            })
            .ToListAsync(ct);

        return Results.Ok(suppliers);
    }
}
