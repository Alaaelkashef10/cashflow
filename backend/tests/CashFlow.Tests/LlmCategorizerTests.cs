using System.Net;
using System.Text;
using System.Text.Json;

using CashFlow.Core.Categorization;
using CashFlow.Core.Entities;
using CashFlow.Infrastructure.Categorization;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CashFlow.Tests;

public class LlmCategorizerTests
{
    private sealed class StubHandler(Func<HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(respond());
        }
    }

    private sealed class ThrowingCategorizer : ICategorizer
    {
        public Task<CategorySuggestion> SuggestAsync(string description, CancellationToken ct = default) =>
            throw new HttpRequestException("boom");
    }

    private sealed class FixedCategorizer(string category) : ICategorizer
    {
        public Task<CategorySuggestion> SuggestAsync(string description, CancellationToken ct = default) =>
            Task.FromResult(new CategorySuggestion(category, 1, "fixed"));
    }

    // Shaped like the real API response: the model's text sits in content[0].text.
    private static HttpResponseMessage Reply(string modelText, HttpStatusCode status = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(new { content = new[] { new { type = "text", text = modelText } } });
        return new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }

    private static LlmCategorizer Create(StubHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.example.test") };
        var options = Options.Create(new AiOptions { ApiKey = "test-key", Model = "test-model" });
        return new LlmCategorizer(http, options);
    }

    [Fact]
    public async Task Parses_a_valid_answer_and_sends_the_api_key()
    {
        var handler = new StubHandler(() => Reply("""{"category":"Utilities","confidence":0.93}"""));

        var result = await Create(handler).SuggestAsync("Vodafone recharge");

        Assert.Equal("Utilities", result.Category);
        Assert.Equal(0.93, result.Confidence);
        Assert.Equal(LlmCategorizer.SourceName, result.Source);
        Assert.Equal("test-key", handler.LastRequest!.Headers.GetValues("x-api-key").Single());
        Assert.Equal("/v1/messages", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task Accepts_json_wrapped_in_a_code_fence()
    {
        var handler = new StubHandler(() => Reply("```json\n{\"category\":\"Rent\",\"confidence\":0.8}\n```"));

        var result = await Create(handler).SuggestAsync("Flat payment");

        Assert.Equal("Rent", result.Category);
    }

    [Fact]
    public async Task Missing_confidence_defaults_to_0_8()
    {
        var handler = new StubHandler(() => Reply("""{"category":"Groceries"}"""));

        var result = await Create(handler).SuggestAsync("Carrefour");

        Assert.Equal(0.8, result.Confidence);
    }

    [Fact]
    public async Task Unknown_category_is_rejected()
    {
        var handler = new StubHandler(() => Reply("""{"category":"Entertainment","confidence":0.9}"""));

        await Assert.ThrowsAsync<InvalidOperationException>(() => Create(handler).SuggestAsync("Cinema"));
    }

    [Fact]
    public async Task Text_that_is_not_json_is_rejected()
    {
        var handler = new StubHandler(() => Reply("I think this is probably a utility bill."));

        await Assert.ThrowsAsync<InvalidOperationException>(() => Create(handler).SuggestAsync("Wifi"));
    }

    [Fact]
    public async Task Server_errors_are_surfaced_as_exceptions()
    {
        var handler = new StubHandler(() => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        await Assert.ThrowsAsync<HttpRequestException>(() => Create(handler).SuggestAsync("Wifi"));
    }

    [Fact]
    public void The_prompt_mentions_every_category()
    {
        Assert.All(Categories.All, c => Assert.Contains(c, LlmCategorizer.SystemPrompt));
    }

    [Fact]
    public async Task Fallback_uses_keyword_rules_when_the_primary_fails()
    {
        var sut = new FallbackCategorizer(
            new ThrowingCategorizer(), new KeywordCategorizer(), NullLogger<FallbackCategorizer>.Instance);

        var result = await sut.SuggestAsync("Netflix monthly");

        Assert.Equal("Subscriptions", result.Category);
        Assert.Equal(KeywordCategorizer.SourceName, result.Source);
    }

    [Fact]
    public async Task Fallback_returns_the_primary_answer_when_it_works()
    {
        var sut = new FallbackCategorizer(
            new FixedCategorizer("Rent"), new KeywordCategorizer(), NullLogger<FallbackCategorizer>.Instance);

        var result = await sut.SuggestAsync("Netflix monthly");

        Assert.Equal("Rent", result.Category);
        Assert.Equal("fixed", result.Source);
    }
}