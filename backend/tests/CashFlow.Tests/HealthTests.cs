using System.Net;

using Microsoft.AspNetCore.Mvc.Testing;

namespace CashFlow.Tests;

public class HealthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_returns_200()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Ping_returns_200()
    {
        var response = await _client.GetAsync("/api/v1/ping");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}