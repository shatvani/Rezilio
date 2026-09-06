using Rezilio.Modules.RiskRegister.Application.Services;

namespace Rezilio.Modules.RiskRegister.Application.Commands.CreateRisk;

public sealed class CreateRiskHandler
{
    private const int MaxCodeGenerationAttempts = 3;

    private readonly RiskRegisterDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public CreateRiskHandler(
        RiskRegisterDbContext db,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _db = db;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    /// <summary>Jogosultság: RiskOwner (bárki jelezhet kockázatot, ld. §1.7/2. lépés).</summary>
    [WolverinePost("/api/riskregister/risks")]
    [Authorize(Roles = "RiskOwner")]
    public async Task<IResult> Handle(CreateRiskCommand command, CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        var auditUser = _currentUserContext.Email ?? _currentUserContext.UserId ?? "unknown";

        for (int attempt = 1; attempt <= MaxCodeGenerationAttempts; attempt++)
        {
            var code = await RiskCodeGenerator.GenerateNextCodeAsync(_db, tenantId, ct);

            var risk = Risk.Create(tenantId, code, command.Domain, command.Title, command.Description, command.OwnerId);
            risk.CreatedAt = DateTimeOffset.UtcNow;
            risk.CreatedBy = auditUser;

            _db.Risks.Add(risk);

            try
            {
                await _db.SaveChangesAsync(ct);
                return Results.Created($"/api/riskregister/risks/{risk.Id}", new { risk.Id, risk.Code });
            }
            catch (DbUpdateException) when (attempt < MaxCodeGenerationAttempts)
            {
                // Ritka verseny-helyzet: két párhuzamos kérés ugyanazt a Code-ot generálta
                // (ld. §3.1 "race condition nélküli tenant-szintű számláló" megjegyzés).
                // A (TenantId, Code) unique index megakadályozza a duplikátumot — újrapróbáljuk
                // friss számlálóval. A tracked entitást le kell választani, különben az EF a
                // következő próbálkozásnál is ugyanazt próbálná beszúrni.
                _db.Entry(risk).State = EntityState.Detached;
            }
        }

        return Results.Conflict("A Risk Code generálása nem sikerült — próbáld újra.");
    }
}
