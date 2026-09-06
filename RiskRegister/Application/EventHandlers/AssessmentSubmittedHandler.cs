using Rezilio.Modules.RiskRegister.Domain.Events;

namespace Rezilio.Modules.RiskRegister.Application.EventHandlers;

/// <summary>
/// Modulon belüli Wolverine local handler — az AssessmentSubmitted eseményből váltja ki a
/// Risk.Status → UnderReview átmenetet (ld. docs/design/risk-register-design.md §2.5/§3.3).
/// Az automatikus felfedezést a Program.cs-ben már regisztrált
/// opts.Discovery.IncludeAssembly(typeof(RiskRegisterModule).Assembly) biztosítja.
///
/// FONTOS, dokumentált v1-korlátozás: ez a SaveChangesAsync hívás egy KÜLÖN tranzakcióban
/// fut, mint az Assessment-et Submitted-be állító Command handler SaveChangesAsync-je —
/// nincs transactional outbox/EF-enrollment bekötve (nincs WolverineFx.EntityFrameworkCore
/// csomag, nincs EnrollDbContextInOutbox hívás sehol a kódbázisban, ld. §2.5). Elméletben
/// tehát előfordulhat, hogy az Assessment Submitted-be kerül, de a Risk.Status-átmenet emiatt
/// nem fut le (pl. a folyamat összeomlik a két SaveChanges között). Ez tudatosan elfogadott
/// v1 kockázat — a teljes atomicitás (outbox pattern) egy jövőbeli, külön megtervezendő fázis.
/// </summary>
public static class AssessmentSubmittedHandler
{
    public static async Task Handle(AssessmentSubmitted @event, RiskRegisterDbContext db, CancellationToken ct)
    {
        var risk = await db.Risks
            .FirstOrDefaultAsync(r => r.Id == @event.RiskId && r.TenantId == @event.TenantId, ct);

        if (risk is null)
        {
            // Nem várt állapot — a Command handler validátora már ellenőrizte a RiskId
            // létezését az Assessment létrehozásakor. Ha mégis idáig jutunk, csendben
            // kilépünk ahelyett, hogy kivétellel megállítanánk a Wolverine feldolgozást.
            return;
        }

        risk.BeginReview();
        await db.SaveChangesAsync(ct);
    }
}
