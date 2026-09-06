using Rezilio.Modules.RiskRegister.Application.Services;

namespace Rezilio.Modules.RiskRegister.Application.Commands.CreateRiskFromArchivedRisk;

public sealed class CreateRiskFromArchivedRiskHandler
{
    private const int MaxCodeGenerationAttempts = 3;

    private readonly RiskRegisterDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public CreateRiskFromArchivedRiskHandler(
        RiskRegisterDbContext db,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _db = db;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    /// <summary>
    /// Jogosultság: RiskOwner (ld. §3.1). A forrás Risk-nek Archived állapotúnak kell
    /// lennie — ezt maga a Risk.CreateFromArchived domain-metódus ellenőrzi.
    /// </summary>
    [WolverinePost("/api/riskregister/risks/{sourceRiskId}/create-from-archived")]
    [Authorize(Roles = "RiskOwner")]
    public async Task<IResult> Handle(
        [FromRoute] Guid sourceRiskId,
        CreateRiskFromArchivedRiskCommand command,
        CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;

        var source = await _db.Risks
            .FirstOrDefaultAsync(r => r.Id == sourceRiskId && r.TenantId == tenantId, ct);

        if (source is null)
        {
            return Results.NotFound();
        }

        var auditUser = _currentUserContext.Email ?? _currentUserContext.UserId ?? "unknown";

        for (int attempt = 1; attempt <= MaxCodeGenerationAttempts; attempt++)
        {
            var code = await RiskCodeGenerator.GenerateNextCodeAsync(_db, tenantId, ct);

            Risk newRisk;
            try
            {
                newRisk = Risk.CreateFromArchived(source, code, command.Domain, command.Title, command.Description);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(ex.Message);
            }

            newRisk.CreatedAt = DateTimeOffset.UtcNow;
            newRisk.CreatedBy = auditUser;

            _db.Risks.Add(newRisk);

            try
            {
                await _db.SaveChangesAsync(ct);
                return Results.Created($"/api/riskregister/risks/{newRisk.Id}", new { newRisk.Id, newRisk.Code });
            }
            catch (DbUpdateException) when (attempt < MaxCodeGenerationAttempts)
            {
                _db.Entry(newRisk).State = EntityState.Detached;
            }
        }

        return Results.Conflict("A Risk Code generálása nem sikerült — próbáld újra.");
    }
}
