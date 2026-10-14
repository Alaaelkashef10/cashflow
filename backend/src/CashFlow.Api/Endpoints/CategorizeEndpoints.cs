using CashFlow.Api.Contracts;
using CashFlow.Api.Validation;
using CashFlow.Core.Categorization;

using Microsoft.AspNetCore.Http.HttpResults;

namespace CashFlow.Api.Endpoints;

public static class CategorizeEndpoints
{
    public static RouteGroupBuilder MapCategorizeEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/categorize", CategorizeAsync)
            .WithTags("Categorize")
            .WithValidation<CategorizeRequest>()
            .RequireRateLimiting("categorize");

        return api;
    }

    private static async Task<Ok<CategorizeResponse>> CategorizeAsync(
        CategorizeRequest request,
        ICategorizer categorizer,
        CancellationToken ct)
    {
        var suggestion = await categorizer.SuggestAsync(request.Description, ct);
        return TypedResults.Ok(
            new CategorizeResponse(suggestion.Category, suggestion.Confidence, suggestion.Source));
    }
}