namespace Rezilio.Modules.Organization.Application.Commands.LinkKeyPersonToUser;

public sealed class LinkKeyPersonToUserHandler
{
    private readonly OrganizationDbContext _db;
    private readonly ITenantContext _tenantContext;

    public LinkKeyPersonToUserHandler(OrganizationDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [WolverinePost("/api/organization/key-persons/{KeyPersonId}/link-user")]
    [Authorize(Roles = "Admin")]
    public async Task<IResult> Handle(LinkKeyPersonToUserCommand command, CancellationToken ct)
    {
        var keyPerson = await _db.KeyPersons
            .FirstOrDefaultAsync(k => k.Id == command.KeyPersonId && k.TenantId == _tenantContext.TenantId, ct);

        if (keyPerson is null)
        {
            return Results.NotFound();
        }

        bool userAlreadyLinkedElsewhere = await _db.KeyPersons
            .AnyAsync(k => k.TenantId == _tenantContext.TenantId
                        && k.UserId == command.UserId
                        && k.Id != command.KeyPersonId, ct);

        if (userAlreadyLinkedElsewhere)
        {
            return Results.Conflict("Ez a felhasználó már egy másik KeyPerson-hoz van kötve.");
        }

        keyPerson.LinkToUser(command.UserId);
        await _db.SaveChangesAsync(ct);

        return Results.Ok(new { keyPerson.Id, keyPerson.UserId });
    }
}
