using Rezilio.Modules.RiskRegister.Domain.Events;

namespace Rezilio.Modules.RiskRegister.Application.EventHandlers;

/// <summary>
/// Modulon belüli Wolverine local handler — az AssessmentRejected eseményből dönti el a
/// Risk.Status visszaesését: Draft-ba, ha ez volt az adott Risk ELSŐ (soha jóvá nem hagyott)
/// Assessment-je, egyébként Active-ba (a korábbi jóváhagyott Assessment marad érvényben).
/// Ld. docs/design/risk-register-design.md §2.2/§3.3.
///
/// Ugyanaz a dokumentált v1-korlátozás vonatkozik erre is, mint a másik két Assessment
/// event-handlerre (ld. AssessmentSubmittedHandler XML-doksi).
/// </summary>
public static class AssessmentRejectedHandler
{
    public static async Task Handle(AssessmentRejected @event, RiskRegisterDbContext db, CancellationToken ct)
    {
        var risk = await db.Risks
            .FirstOrDefaultAsync(r => r.Id == @event.RiskId && r.TenantId == @event.TenantId, ct);

        if (risk is null)
        {
            return;
        }

        bool hasPriorApprovedAssessment = await db.Assessments.AnyAsync(
            a => a.RiskId == @event.RiskId && a.TenantId == @event.TenantId && a.ApprovalStatus == ApprovalStatus.Approved,
            ct);

        risk.RevertAfterRejection(hasPriorApprovedAssessment);
        await db.SaveChangesAsync(ct);
    }
}
