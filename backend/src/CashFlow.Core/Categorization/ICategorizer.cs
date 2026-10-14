namespace CashFlow.Core.Categorization;

public sealed record CategorySuggestion(string Category, double Confidence, string Source);

public interface ICategorizer
{
    Task<CategorySuggestion> SuggestAsync(string description, CancellationToken ct = default);
}