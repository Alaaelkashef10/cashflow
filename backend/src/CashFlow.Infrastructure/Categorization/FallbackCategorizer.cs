using CashFlow.Core.Categorization;

using Microsoft.Extensions.Logging;

namespace CashFlow.Infrastructure.Categorization;

public sealed class FallbackCategorizer(
    ICategorizer primary,
    ICategorizer fallback,
    ILogger<FallbackCategorizer> logger) : ICategorizer
{
    public async Task<CategorySuggestion> SuggestAsync(string description, CancellationToken ct = default)
    {
        try
        {
            return await primary.SuggestAsync(description, ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            // The description is deliberately not logged.
            logger.LogWarning(ex, "The AI categorizer failed; using keyword rules instead.");
            return await fallback.SuggestAsync(description, ct);
        }
    }
}