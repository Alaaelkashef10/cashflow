namespace CashFlow.Api.Contracts;

public sealed record CreateScenarioRequest(string Name, decimal StartingBalance, DateOnly StartDate);

// PATCH: every field is optional; only the ones you send are changed.
public sealed record UpdateScenarioRequest(string? Name, decimal? StartingBalance, DateOnly? StartDate);

public sealed record ScenarioResponse(
    int Id,
    string Name,
    decimal StartingBalance,
    DateOnly StartDate,
    int EntryCount);