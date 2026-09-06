namespace Rezilio.Modules.RiskRegister.Application.Queries.ListRisks;

public sealed record PagedRiskListResult(IReadOnlyList<RiskListItemDto> Items, int TotalCount, int Page, int PageSize);
