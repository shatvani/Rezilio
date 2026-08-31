namespace Rezilio.Modules.Organization.Application.Commands.DeleteCustomer;

public sealed class DeleteCustomerHandler(OrganizationDbContext db, ITenantContext tenantContext)
{
    [WolverineDelete("/api/organization/customers/{id}")]
    [Authorize]
    public async Task<IResult> Handle([FromRoute] Guid id, CancellationToken ct)
    {
        var customer = await db.Customers
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantContext.TenantId, ct);
        if (customer is null)
        {
            return Results.NotFound($"Customer {id} nem található.");
        }

        db.Customers.Remove(customer);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
