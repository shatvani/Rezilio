using Rezilio.SharedKernel.DDD.VOs;

namespace Rezilio.Modules.Organization.Application.Commands.SetBusinessProcessAnnualEbitda;

public sealed class SetBusinessProcessAnnualEbitdaHandler(OrganizationDbContext db, ITenantContext tenantContext)
{
    [WolverinePost("/api/organization/business-processes/{BusinessProcessId}/annual-ebitda")]
    [Authorize(Roles = "Admin")]
    public async Task<IResult> Handle(SetBusinessProcessAnnualEbitdaCommand command, CancellationToken ct)
    {
        var businessProcess = await db.BusinessProcesses
            .FirstOrDefaultAsync(b => b.Id == command.BusinessProcessId && b.TenantId == tenantContext.TenantId, ct);

        if (businessProcess is null)
        {
            return Results.NotFound();
        }

        var ebitda = command.Amount is { } amount
            ? new Money(amount, new CurrencyCode(command.Currency!))
            : null;

        businessProcess.SetAnnualEbitda(ebitda);
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
