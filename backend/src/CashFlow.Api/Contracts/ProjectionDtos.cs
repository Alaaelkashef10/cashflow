namespace CashFlow.Api.Contracts;

public sealed record ProjectionDayResponse(
    DateOnly Date,
    decimal ClosingBalance,
    bool IsWarning,
    IReadOnlyList<EntryResponse> Entries);

public sealed record ProjectionSummary(
    decimal LowestBalance,
    DateOnly LowestBalanceDate,
    int WarningDayCount,
    decimal EndingBalance);

public sealed record ProjectionResponse(
    int ScenarioId,
    decimal StartingBalance,
    DateOnly StartDate,
    DateOnly EndDate,
    IReadOnlyList<ProjectionDayResponse> Days,
    IReadOnlyList<DateOnly> WarningDates,
    ProjectionSummary Summary);