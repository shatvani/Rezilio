namespace Rezilio.Modules.RiskRegister.Application.Queries.ListFindings;

public sealed record PagedFindingListResult(IReadOnlyList<FindingListItemDto> Items, int TotalCount, int Page, int PageSize);
