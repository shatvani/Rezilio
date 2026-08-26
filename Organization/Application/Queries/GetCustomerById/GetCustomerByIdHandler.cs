using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rezilio.Modules.Organization.Infrastructure;
using Wolverine.Http;

namespace Rezilio.Modules.Organization.Application.Queries.GetCustomerById;

public sealed class GetCustomerByIdHandler(OrganizationDbContext db)
{
    [WolverineGet("/api/organization/customers/{id}")]
    [Authorize]
    public async Task<IResult> Handle([FromRoute] Guid id, CancellationToken ct)
    {
        var customer = await db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (customer is null)
        {
            return Results.NotFound($"Customer {id} nem található.");
        }

        return Results.Ok(new
        {
            customer.Id,
            customer.TenantId,
            customer.Name,
            customer.Code,
            customer.Industry,
            customer.Country,
            customer.ContactEmail,
            customer.ContactPhone,
            customer.Description
        });
    }
}
