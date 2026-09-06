namespace Rezilio.Modules.RiskRegister.Application.Queries.GetAssessmentById;

public sealed class GetAssessmentByIdHandler(RiskRegisterDbContext db, ITenantContext tenantContext)
{
    [WolverineGet("/api/riskregister/assessments/{id}")]
    [Authorize]
    public async Task<IResult> Handle([FromRoute] Guid id, CancellationToken ct)
    {
        // Nincs .Select()-es projekció — az opcionális (nullable) owned Money VO-k
        // EF Core-beli lekérdezés-fordítása .Select()-ben törékeny; ehelyett a teljes
        // entitást töltjük be (AsNoTracking), és memóriában map-eljük a DTO-ra
        // (ugyanaz a minta, mint a GetTenantSettingsHandler-nél, Organization modul).
        var assessment = await db.Assessments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantContext.TenantId, ct);

        if (assessment is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(AssessmentDto.FromEntity(assessment));
    }
}
