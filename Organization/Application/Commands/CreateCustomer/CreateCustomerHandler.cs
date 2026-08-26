using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Rezilio.Modules.Organization.Domain;
using Rezilio.Modules.Organization.Infrastructure;
using Wolverine.Http;

namespace Rezilio.Modules.Organization.Application.Commands.CreateCustomer;

public sealed class CreateCustomerHandler(OrganizationDbContext db)
{
    [WolverinePost("/api/organization/customers")]
    [Authorize]
    public async Task<IResult> Handle(CreateCustomerCommand command, CancellationToken ct)
    {
        var customer = Customer.Create(
            command.TenantId,
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
