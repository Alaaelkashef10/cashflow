using CashFlow.Core.Entities;

namespace CashFlow.Api.Contracts;

public sealed record CreateEntryRequest(
    EntryType Type,
    string Description,
    decimal Amount,
    DateOnly Date,
    string? Category,
    string? AiSuggestedCategory);

// PATCH: send only the fields you want to change.
public sealed record UpdateEntryRequest(
    EntryType? Type,
    string? Description,
    decimal? Amount,
    DateOnly? Date,
    string? Category);

public sealed record EntryResponse(
    int Id,
    int ScenarioId,
    EntryType Type,
    string Description,
    decimal Amount,
    DateOnly Date,
    string Category,
    string? AiSuggestedCategory,
    bool UserCorrected);

public static class EntryMapper
{
    public static EntryResponse ToResponse(Entry e) => new(
        e.Id, e.ScenarioId, e.Type, e.Description, e.Amount, e.Date,
        e.Category, e.AiSuggestedCategory, e.UserCorrected);
}