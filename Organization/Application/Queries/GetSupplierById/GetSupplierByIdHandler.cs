namespace Rezilio.Modules.Organization.Application.Queries.GetSupplierById;

public sealed class GetSupplierByIdHandler(OrganizationDbContext db)
{
    [WolverineGet("/api/organization/suppliers/{id}")]
    [Authorize]
    public async Task<IResult> Handle([FromRoute] Guid id, CancellationToken ct)
    {
        var supplier = await db.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (supplier is null)
        {
            return Results.NotFound($"Supplier {id} nem található.");
        }

        return Results.Ok(new
        {
            supplier.Id,
            supplier.TenantId,
            supplier.Name,
            supplier.Code,
            supplier.Industry,
            supplier.Country,
            supplier.ContactEmail,
            supplier.ContactPhone,
            supplier.Description
        });
    }
}
