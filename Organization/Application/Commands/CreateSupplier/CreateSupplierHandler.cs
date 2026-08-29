namespace Rezilio.Modules.Organization.Application.Commands.CreateSupplier;

public sealed class CreateSupplierHandler(OrganizationDbContext db)
{
    [WolverinePost("/api/organization/suppliers")]
    [Authorize]
    public async Task<IResult> Handle(CreateSupplierCommand command, CancellationToken ct)
    {
        bool codeExists = await db.Suppliers
            .AnyAsync(s => s.TenantId == command.TenantId
                        && s.Code == command.Code.Trim().ToUpperInvariant(), ct);

        if (codeExists)
        {
            return Results.Conflict($"Supplier with code '{command.Code}' already exists.");
        }

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
