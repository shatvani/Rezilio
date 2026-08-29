namespace Rezilio.Modules.Organization.Application.Commands.DeleteSupplier;

public sealed class DeleteSupplierHandler(OrganizationDbContext db)
{
    [WolverineDelete("/api/organization/suppliers/{id}")]
    [Authorize]
    public async Task<IResult> Handle([FromRoute] Guid id, CancellationToken ct)
    {
        var supplier = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (supplier is null)
        {
            return Results.NotFound($"Supplier {id} nem található.");
        }

        db.Suppliers.Remove(supplier);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
