namespace Rezilio.Modules.Organization.Application.Commands.CreateCustomer;

public sealed class CreateCustomerHandler(OrganizationDbContext db, ITenantContext tenantContext)
{
    [WolverinePost("/api/organization/customers")]
    [Authorize]
    public async Task<IResult> Handle(CreateCustomerCommand command, CancellationToken ct)
    {
        bool codeExists = await db.Customers
            .AnyAsync(c => c.TenantId == tenantContext.TenantId
                        && c.Code == command.Code.Trim().ToUpperInvariant(), ct);

        if (codeExists)
        {
            return Results.Conflict($"Customer with code '{command.Code}' already exists.");
        }

        var customer = Customer.Create(
            tenantContext.TenantId,
            command.Name,
            command.Code,
            command.Industry,
            command.Country,
            command.ContactEmail,
            command.ContactPhone,
            command.Description);

        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/organization/customers/{customer.Id}", new { customer.Id });
    }
}
