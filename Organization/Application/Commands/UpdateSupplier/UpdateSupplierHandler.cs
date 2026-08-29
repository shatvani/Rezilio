namespace Rezilio.Modules.Organization.Application.Commands.UpdateSupplier;

public sealed class UpdateSupplierHandler(OrganizationDbContext db)
{
    [WolverinePut("/api/organization/suppliers/{id}")]
    [Authorize]
    public async Task<IResult> Handle([FromRoute] Guid id, UpdateSupplierCommand command, CancellationToken ct)
    {
        var supplier = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (supplier is null)
        {
            return Results.NotFound($"Supplier {id} nem található.");
        }

        bool codeExists = await db.Suppliers
            .AnyAsync(s => s.TenantId == supplier.TenantId
                        && s.Code == command.Code.Trim().ToUpperInvariant()
                        && s.Id != id, ct);

        if (codeExists)
        {
            return Results.Conflict($"Supplier with code '{command.Code}' already exists.");
        }

        supplier.Update(
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
