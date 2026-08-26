using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rezilio.Modules.Organization.Infrastructure;
using Wolverine.Http;

namespace Rezilio.Modules.Organization.Application.Commands.UpdateCustomer;

public sealed class UpdateCustomerHandler(OrganizationDbContext db)
{
    [WolverinePut("/api/organization/customers/{id}")]
    [Authorize]
    public async Task<IResult> Handle([FromRoute] Guid id, UpdateCustomerCommand command, CancellationToken ct)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (customer is null)
        {
            return Results.NotFound($"Customer {id} nem található.");
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
