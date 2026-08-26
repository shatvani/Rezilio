using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Rezilio.Modules.Organization.Domain;
using Rezilio.Modules.Organization.Infrastructure;
using Wolverine.Http;

namespace Rezilio.Modules.Organization.Application.Commands.CreateSupplier;

public sealed class CreateSupplierHandler(OrganizationDbContext db)
{
    [WolverinePost("/api/organization/suppliers")]
    [Authorize]
    public async Task<IResult> Handle(CreateSupplierCommand command, CancellationToken ct)
    {
        var supplier = Supplier.Create(
            command.TenantId,
            command.Name,
            command.Code,
            command.Industry,
            command.Country,
            command.ContactEmail,
            command.ContactPhone,
            command.Description);

        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/organization/suppliers/{supplier.Id}", new { supplier.Id });
    }
}
