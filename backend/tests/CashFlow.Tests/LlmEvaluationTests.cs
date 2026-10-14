using CashFlow.Core.Categorization;
using CashFlow.Infrastructure.Categorization;

using Microsoft.Extensions.Options;

using Xunit.Abstractions;

namespace CashFlow.Tests;

public class LlmEvaluationTests(ITestOutputHelper output)
{
    private sealed record Row(string Description, string Category);

    private static string? Env(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static List<Row> LoadTestRows()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "ai", "data", "descriptions.csv");
            if (File.Exists(candidate))
            {
                return File.ReadLines(candidate)
                    .Skip(1)
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .Select(l => l.Split(','))
                    .Where(p => p[0].Trim() == "test")
                    .Select(p => new Row(p[1].Trim(), p[2].Trim()))
                    .ToList();
            }
            dir = dir.Parent;
        }
        throw new FileNotFoundException("Could not find ai/data/descriptions.csv.");
    }

    [Fact]
    public async Task Llm_versus_keyword_on_the_test_split()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        ICategorizer llm;
        string label;

        var azureKey = Env("AZURE_OPENAI_API_KEY");
        var azureEndpoint = Env("AZURE_OPENAI_ENDPOINT");
        var azureDeployment = Env("AZURE_OPENAI_DEPLOYMENT_FAST");
        var anthropicKey = Env("ANTHROPIC_API_KEY");

        if (azureKey is not null && azureEndpoint is not null && azureDeployment is not null)
        {
            http.BaseAddress = new Uri(azureEndpoint.TrimEnd('/') + "/");
            llm = new AzureOpenAiCategorizer(http, Options.Create(new AiOptions
            {
                AzureApiKey = azureKey,
                AzureEndpoint = azureEndpoint,
                AzureDeployment = azureDeployment
            }));
            label = $"Azure OpenAI, deployment {azureDeployment}";
        }
        else if (anthropicKey is not null)
        {
            var settings = new AiOptions { ApiKey = anthropicKey };
            http.BaseAddress = new Uri(settings.BaseUrl);
            llm = new LlmCategorizer(http, Options.Create(settings));
            label = $"Anthropic, model {settings.Model}";
        }
        else
        {
            output.WriteLine("SKIPPED: set AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_API_KEY and AZURE_OPENAI_DEPLOYMENT_FAST (or ANTHROPIC_API_KEY).");
            return;
        }

        var keyword = new KeywordCategorizer();
        var results = new List<(Row Row, string Keyword, string Llm)>();
        string? firstError = null;

        foreach (var row in LoadTestRows())
        {
            string llmCategory;
            try
            {
                llmCategory = (await llm.SuggestAsync(row.Description)).Category;
            }
            catch (Exception ex)
            {
                firstError ??= ex.Message;
                llmCategory = "ERROR";
            }

            results.Add((row, keyword.Suggest(row.Description).Category, llmCategory));
        }

        double Accuracy(Func<(Row Row, string Keyword, string Llm), string> pick) =>
            results.Count(r => pick(r) == r.Row.Category) / (double)results.Count;

        var keywordAccuracy = Accuracy(r => r.Keyword);
        var llmAccuracy = Accuracy(r => r.Llm);

        output.WriteLine($"MODEL: {label}");
        output.WriteLine($"Test rows:        {results.Count}");
        output.WriteLine($"Keyword accuracy: {keywordAccuracy:P1}");
        output.WriteLine($"LLM accuracy:     {llmAccuracy:P1}");
        if (firstError is not null) output.WriteLine($"First LLM error:  {firstError}");

        output.WriteLine("");
        output.WriteLine("LLM errors (expected -> predicted):");
        foreach (var r in results.Where(r => r.Llm != r.Row.Category))
            output.WriteLine($"  \"{r.Row.Description}\": {r.Row.Category} -> {r.Llm}");

        output.WriteLine("");
        output.WriteLine("Rows the LLM fixed (keyword was wrong, LLM is right):");
        foreach (var r in results.Where(r => r.Keyword != r.Row.Category && r.Llm == r.Row.Category))
            output.WriteLine($"  \"{r.Row.Description}\": {r.Keyword} -> {r.Llm}");

        Assert.True(llmAccuracy >= 0.80, $"LLM accuracy was {llmAccuracy:P1}.");
    }
}