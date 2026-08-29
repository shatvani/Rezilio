namespace Rezilio.Modules.Organization.Application.Commands.DeleteItSystem;

public sealed class DeleteItSystemHandler
{
    private readonly OrganizationDbContext _db;

    public DeleteItSystemHandler(OrganizationDbContext db)
    {
        _db = db;
    }

    [WolverineDelete("/api/organization/it-systems/{id}")]
    [Authorize]
    public async Task<IResult> Handle([FromRoute] Guid id, CancellationToken ct)
    {
        ItSystem? system = await _db.ItSystems.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (system is null)
        {
            return Results.NotFound();
        }

        _db.ItSystems.Remove(system);
        await _db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
