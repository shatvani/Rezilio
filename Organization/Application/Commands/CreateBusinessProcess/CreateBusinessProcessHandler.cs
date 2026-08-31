namespace Rezilio.Modules.Organization.Application.Commands.CreateBusinessProcess;

public sealed class CreateBusinessProcessHandler
{
    private readonly OrganizationDbContext _db;
    private readonly ITenantContext _tenantContext;

    public CreateBusinessProcessHandler(OrganizationDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [WolverinePost("/api/organization/business-processes")]
    [Authorize]
    public async Task<IResult> Handle(CreateBusinessProcessCommand command, CancellationToken ct)
    {
        bool codeExists = await _db.BusinessProcesses
            .AnyAsync(b => b.TenantId == _tenantContext.TenantId
                        && b.Code == command.Code.Trim().ToUpperInvariant(), ct);

        if (codeExists)
        {
            return Results.Conflict($"Business process with code '{command.Code}' already exists.");
        }

        var result = BusinessProcess.Create(
            _tenantContext.TenantId,
            command.Code,
            command.Name,
            command.Category,
            command.CriticalityLevel,
            command.OwnerId,
            command.OrgUnitId,
            command.MaxTolerableDowntimeMinutes,
            command.RecoveryTimeObjectiveMinutes,
            command.DependsOnSystemIds);

        if (result.IsFailure)
        {
            return Results.BadRequest(result.Error);
        }

        _db.BusinessProcesses.Add(result.Value);
        await _db.SaveChangesAsync(ct);

        return Results.Created($"/api/organization/business-processes/{result.Value.Id}", new { result.Value.Id });
    }
}
