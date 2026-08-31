namespace Rezilio.Modules.Organization.Application.Queries.GetCustomersByTenant;

public sealed class GetCustomersByTenantHandler(OrganizationDbContext db, ITenantContext tenantContext)
{
    [WolverineGet("/api/organization/customers")]
    [Authorize]
    public async Task<IResult> Handle(CancellationToken ct)
    {
        var customers = await db.Customers
            .AsNoTracking()
            .Where(c => c.TenantId == tenantContext.TenantId)
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Code,
                c.Industry,
                c.Country,
                c.ContactEmail,
                c.ContactPhone,
                c.Description
            })
            .ToListAsync(ct);

        return Results.Ok(customers);
    }
}
