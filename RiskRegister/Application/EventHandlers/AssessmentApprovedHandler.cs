using Rezilio.Modules.RiskRegister.Domain.Events;
using Rezilio.Modules.RiskRegister.Domain.Services;
using Rezilio.SharedKernel.Contracts.RiskRegister;
using Wolverine;

namespace Rezilio.Modules.RiskRegister.Application.EventHandlers;

/// <summary>
/// Modulon belüli Wolverine local handler — az AssessmentApproved eseményből váltja ki a
/// Risk.Status → Active átmenetet (üzleti szabály #1: Approve MINDIG Active-ba viszi a
/// Risk-et, függetlenül a kockázat-étvágy kimenetelétől), majd a saját SaveChangesAsync-je
/// után kiküldi a modulhatáron átnyúló RiskScoreChanged eseményt (ld. §2.2/§3.3).
///
/// Ugyanaz a dokumentált v1-korlátozás vonatkozik erre is, mint az
/// AssessmentSubmittedHandler-re: nincs transactional outbox, a Risk-mentés és az
/// Assessment-mentés két külön tranzakció (ld. §2.5).
/// </summary>
public static class AssessmentApprovedHandler
{
    public static async Task Handle(AssessmentApproved @event, RiskRegisterDbContext db, IMessageBus messageBus, CancellationToken ct)
    {
        var risk = await db.Risks
            .FirstOrDefaultAsync(r => r.Id == @event.RiskId && r.TenantId == @event.TenantId, ct);

        if (risk is null)
        {
            return;
        }

        risk.ActivateAfterApproval();
        await db.SaveChangesAsync(ct);

        var band = RiskScoreCalculator.CalculateBand(@event.ResidualScore);
        await messageBus.PublishAsync(new RiskScoreChanged(@event.TenantId, @event.RiskId, @event.ResidualScore, band.ToString()));
    }
}
