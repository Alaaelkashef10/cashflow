namespace CashFlow.Infrastructure.Categorization;

public sealed class AiOptions
{
    // "keyword" (default), "azure-openai", or "llm" (Anthropic)
    public string Provider { get; set; } = "keyword";

    // Anthropic
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "claude-haiku-5-5";
    public string BaseUrl { get; set; } = "https://api.anthropic.com";

    // Azure OpenAI
    public string? AzureEndpoint { get; set; }
    public string? AzureApiKey { get; set; }
    public string? AzureDeployment { get; set; }

    public int TimeoutSeconds { get; set; } = 4;

    public bool UseLlm => Is("llm") && !string.IsNullOrWhiteSpace(ApiKey);

    public bool UseAzure =>
        Is("azure-openai")
        && !string.IsNullOrWhiteSpace(AzureEndpoint)
        && !string.IsNullOrWhiteSpace(AzureApiKey)
        && !string.IsNullOrWhiteSpace(AzureDeployment);

    private bool Is(string name) => Provider.Equals(name, StringComparison.OrdinalIgnoreCase);
}