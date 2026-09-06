namespace Rezilio.Modules.RiskRegister.Application.Commands.UpdateRisk;

public sealed class UpdateRiskHandler
{
    private readonly RiskRegisterDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public UpdateRiskHandler(
        RiskRegisterDbContext db,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _db = db;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    /// <summary>
    /// Jogosultság (§3.5): a Risk.OwnerId-jével megegyező KeyPerson, VAGY RiskManager/Admin.
    /// A realm role csak azt dönti el, hogy valaki EGYÁLTALÁN próbálkozhat-e (RiskOwner,
    /// RiskManager vagy Admin realm role kell); a rekord-szintű ellenőrzés a Handler-ben
    /// történik.
    /// </summary>
    [WolverinePut("/api/riskregister/risks/{id}")]
    [Authorize(Roles = "RiskOwner,RiskManager,Admin")]
    public async Task<IResult> Handle([FromRoute] Guid id, UpdateRiskCommand command, CancellationToken ct)
    {
        var risk = await _db.Risks
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == _tenantContext.TenantId, ct);

        if (risk is null)
        {
            return Results.NotFound();
        }

        if (!await IsOwnerOrManagerAsync(risk, ct))
        {
            return Results.Forbid();
        }

        try
        {
            risk.Update(command.Title, command.Description);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ex.Message);
        }

        risk.AuditChanges(_currentUserContext.Email ?? _currentUserContext.UserId ?? "unknown");
        await _db.SaveChangesAsync(ct);

        return Results.NoContent();
    }

    private async Task<bool> IsOwnerOrManagerAsync(Risk risk, CancellationToken ct)
    {
        if (_currentUserContext.IsInRole("RiskManager") || _currentUserContext.IsInRole("Admin"))
        {
            return true;
        }

        var keyPersonId = await _currentUserContext.GetKeyPersonIdAsync(ct);
        return keyPersonId is not null && keyPersonId == risk.OwnerId;
    }
}
