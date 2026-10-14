using System.Net.Http.Json;
using System.Text.Json;

using CashFlow.Core.Categorization;

using Microsoft.Extensions.Options;

namespace CashFlow.Infrastructure.Categorization;

public sealed class AzureOpenAiCategorizer(HttpClient http, IOptions<AiOptions> options) : ICategorizer
{
    public async Task<CategorySuggestion> SuggestAsync(string description, CancellationToken ct = default)
    {
        var settings = options.Value;

        var payload = new
        {
            model = settings.AzureDeployment, // on Azure this is the deployment name
            messages = new object[]
            {
                new { role = "system", content = LlmCategorizer.SystemPrompt },
                new { role = "user", content = $"Description: {description}" }
            },
            temperature = 0,
            max_completion_tokens = 60,
            response_format = new { type = "json_object" }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "openai/v1/chat/completions")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("api-key", settings.AzureApiKey);

        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var text = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";

        // Same safety check as before: the answer must be JSON with a category from our fixed list.
        return LlmCategorizer.Parse(text);
    }
}