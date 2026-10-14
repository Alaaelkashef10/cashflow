using System.Net;
using System.Net.Http.Json;

using CashFlow.Api.Contracts;

namespace CashFlow.Tests.Integration;

[Collection("api")]
public class CategorizeApiTests(ApiFixture fixture)
{
    private static Task<HttpResponseMessage> Post(HttpClient c, string description) =>
        c.PostAsJsonAsync("/api/v1/categorize", new CategorizeRequest(description), ApiFixture.Json);

    [Fact]
    public async Task Returns_a_category_from_the_fixed_list()
    {
        var response = await Post(fixture.CreateClient(), "Netflix monthly");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CategorizeResponse>(ApiFixture.Json);
        Assert.Equal("Subscriptions", body!.Category);
        Assert.Equal("keyword", body.Source);
    }

    [Fact]
    public async Task Unknown_description_is_uncategorized()
    {
        var response = await Post(fixture.CreateClient(), "qwerty zxcv");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CategorizeResponse>(ApiFixture.Json);
        Assert.Equal("Uncategorized", body!.Category);
    }

    [Fact]
    public async Task Empty_description_returns_400()
    {
        var response = await Post(fixture.CreateClient(), "");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}