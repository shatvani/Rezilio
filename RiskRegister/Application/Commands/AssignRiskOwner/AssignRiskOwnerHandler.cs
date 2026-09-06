namespace Rezilio.Modules.RiskRegister.Application.Commands.AssignRiskOwner;

public sealed class AssignRiskOwnerHandler
{
    private readonly RiskRegisterDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public AssignRiskOwnerHandler(
        RiskRegisterDbContext db,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _db = db;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    /// <summary>
    /// Jogosultság: RiskManager/Admin (nem maga a Risk Owner nevezi ki magát, ld. §3.1).
    /// </summary>
    [WolverinePost("/api/riskregister/risks/{id}/owner")]
    [Authorize(Roles = "RiskManager,Admin")]
    public async Task<IResult> Handle([FromRoute] Guid id, AssignRiskOwnerCommand command, CancellationToken ct)
    {
        var risk = await _db.Risks
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == _tenantContext.TenantId, ct);

        if (risk is null)
        {
            return Results.NotFound();
        }

        try
        {
            risk.AssignOwner(command.OwnerId);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ex.Message);
        }

        risk.AuditChanges(_currentUserContext.Email ?? _currentUserContext.UserId ?? "unknown");
        await _db.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
