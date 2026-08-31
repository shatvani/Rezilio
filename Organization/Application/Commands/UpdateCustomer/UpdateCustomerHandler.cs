namespace Rezilio.Modules.Organization.Application.Commands.UpdateCustomer;

public sealed class UpdateCustomerHandler(OrganizationDbContext db, ITenantContext tenantContext)
{
    [WolverinePut("/api/organization/customers/{id}")]
    [Authorize]
    public async Task<IResult> Handle([FromRoute] Guid id, UpdateCustomerCommand command, CancellationToken ct)
    {
        var customer = await db.Customers
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantContext.TenantId, ct);
        if (customer is null)
        {
            return Results.NotFound($"Customer {id} nem található.");
        }

        bool codeExists = await db.Customers
            .AnyAsync(c => c.TenantId == tenantContext.TenantId
                        && c.Code == command.Code.Trim().ToUpperInvariant()
                        && c.Id != id, ct);

        if (codeExists)
        {
            return Results.Conflict($"Customer with code '{command.Code}' already exists.");
        }

        customer.Update(
            command.Name,
            command.Code,
            command.Industry,
            command.Country,
            command.ContactEmail,
            command.ContactPhone,
            command.Description);

        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
