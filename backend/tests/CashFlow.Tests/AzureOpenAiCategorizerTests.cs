using System.Net;
using System.Text;
using System.Text.Json;

using CashFlow.Infrastructure.Categorization;

using Microsoft.Extensions.Options;

namespace CashFlow.Tests;

public class AzureOpenAiCategorizerTests
{
    private sealed class StubHandler(Func<HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return respond();
        }
    }

    // Shaped like the real response: the model's text sits in choices[0].message.content.
    private static HttpResponseMessage Reply(string modelText, HttpStatusCode status = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(new
        {
            choices = new[] { new { message = new { role = "assistant", content = modelText } } }
        });
        return new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }

    private static AzureOpenAiCategorizer Create(StubHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://example-resource.openai.azure.test/") };
        var options = Options.Create(new AiOptions
        {
            AzureApiKey = "test-key",
            AzureDeployment = "my-deployment",
            AzureEndpoint = "https://example-resource.openai.azure.test"
        });
        return new AzureOpenAiCategorizer(http, options);
    }

    [Fact]
    public async Task Sends_the_key_deployment_and_parses_the_answer()
    {
        var handler = new StubHandler(() => Reply("""{"category":"Transport","confidence":0.9}"""));

        var result = await Create(handler).SuggestAsync("Tuk tuk to the station");

        Assert.Equal("Transport", result.Category);
        Assert.Equal(0.9, result.Confidence);
        Assert.Equal(LlmCategorizer.SourceName, result.Source);
        Assert.Equal("test-key", handler.LastRequest!.Headers.GetValues("api-key").Single());
        Assert.Equal("/openai/v1/chat/completions", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"model\":\"my-deployment\"", handler.LastBody);
    }

    [Fact]
    public async Task Unknown_category_is_rejected()
    {
        var handler = new StubHandler(() => Reply("""{"category":"Entertainment","confidence":0.9}"""));

        await Assert.ThrowsAsync<InvalidOperationException>(() => Create(handler).SuggestAsync("Cinema"));
    }

    [Fact]
    public async Task Unauthorized_is_surfaced_as_an_exception()
    {
        var handler = new StubHandler(() => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        await Assert.ThrowsAsync<HttpRequestException>(() => Create(handler).SuggestAsync("Wifi"));
    }
}