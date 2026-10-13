using System.Net;
using System.Net.Http.Json;

using CashFlow.Api.Contracts;
using CashFlow.Core.Entities;
using CashFlow.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CashFlow.Tests.Integration;

[Collection("api")]
public class ApiTests(ApiFixture fixture)
{
    private static readonly System.Text.Json.JsonSerializerOptions Json = ApiFixture.Json;
    private static readonly DateOnly Start = new(2026, 11, 1);

    // ----- helpers -----

    private static async Task<ScenarioResponse> CreateScenarioAsync(HttpClient c, decimal balance = 1000m)
    {
        var r = await c.PostAsJsonAsync("/api/v1/scenarios",
            new CreateScenarioRequest("Test", balance, Start), Json);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<ScenarioResponse>(Json))!;
    }

    private static async Task<EntryResponse> AddEntryAsync(
        HttpClient c, int scenarioId, EntryType type, string description,
        decimal amount, int day, string? category = null, string? ai = null)
    {
        var r = await c.PostAsJsonAsync($"/api/v1/scenarios/{scenarioId}/entries",
            new CreateEntryRequest(type, description, amount, Start.AddDays(day - 1), category, ai), Json);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<EntryResponse>(Json))!;
    }

    private static Task<HttpResponseMessage> PatchAsync(HttpClient c, string url, object body) =>
        c.SendAsync(new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = JsonContent.Create(body, options: Json)
        });

    private static async Task<ProjectionResponse> GetProjectionAsync(HttpClient c, int scenarioId)
    {
        var r = await c.GetAsync($"/api/v1/scenarios/{scenarioId}/projection");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<ProjectionResponse>(Json))!;
    }

    // ----- scenarios -----

    [Fact]
    public async Task Create_and_get_scenario()
    {
        var c = fixture.CreateClient(Guid.NewGuid());
        var created = await CreateScenarioAsync(c);

        var r = await c.GetAsync($"/api/v1/scenarios/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var got = await r.Content.ReadFromJsonAsync<ScenarioResponse>(Json);
        Assert.Equal("Test", got!.Name);
        Assert.Equal(1000m, got.StartingBalance);
    }

    [Fact]
    public async Task Missing_owner_header_returns_400()
    {
        var c = fixture.CreateClient();

        var r = await c.GetAsync("/api/v1/scenarios");

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Invalid_scenario_returns_400()
    {
        var c = fixture.CreateClient(Guid.NewGuid());

        var r = await c.PostAsJsonAsync("/api/v1/scenarios",
            new CreateScenarioRequest("", 1000m, Start), Json);

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Contains("Name", await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Another_owner_cannot_see_or_change_the_data()
    {
        var mine = fixture.CreateClient(Guid.NewGuid());
        var theirs = fixture.CreateClient(Guid.NewGuid());

        var scenario = await CreateScenarioAsync(mine);
        var entry = await AddEntryAsync(mine, scenario.Id, EntryType.Bill, "Rent", 100m, 1, "Rent");

        Assert.Equal(HttpStatusCode.NotFound,
            (await theirs.GetAsync($"/api/v1/scenarios/{scenario.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await theirs.GetAsync($"/api/v1/scenarios/{scenario.Id}/entries")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await theirs.GetAsync($"/api/v1/scenarios/{scenario.Id}/projection")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await PatchAsync(theirs, $"/api/v1/entries/{entry.Id}", new { amount = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await theirs.DeleteAsync($"/api/v1/entries/{entry.Id}")).StatusCode);
    }

    [Fact]
    public async Task Deleting_a_scenario_deletes_its_entries()
    {
        var c = fixture.CreateClient(Guid.NewGuid());
        var scenario = await CreateScenarioAsync(c);
        await AddEntryAsync(c, scenario.Id, EntryType.Bill, "Rent", 100m, 1, "Rent");

        var delete = await c.DeleteAsync($"/api/v1/scenarios/{scenario.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await c.GetAsync($"/api/v1/scenarios/{scenario.Id}/entries")).StatusCode);

        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(0, await db.Entries.CountAsync(e => e.ScenarioId == scenario.Id));
    }

    // ----- entries -----

    [Fact]
    public async Task Invalid_entry_returns_400_with_field_errors()
    {
        var c = fixture.CreateClient(Guid.NewGuid());
        var scenario = await CreateScenarioAsync(c);

        var r = await c.PostAsJsonAsync($"/api/v1/scenarios/{scenario.Id}/entries",
            new CreateEntryRequest(EntryType.Bill, "Bad", 0m, Start, "Nonsense", null), Json);

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var body = await r.Content.ReadAsStringAsync();
        Assert.Contains("Amount", body);
        Assert.Contains("Category", body);
    }

    [Fact]
    public async Task Changing_the_ai_suggested_category_sets_the_corrected_flag()
    {
        var c = fixture.CreateClient(Guid.NewGuid());
        var scenario = await CreateScenarioAsync(c);

        var corrected = await AddEntryAsync(c, scenario.Id, EntryType.Bill, "Netflix", 10m, 1,
            category: "Groceries", ai: "Subscriptions");
        var accepted = await AddEntryAsync(c, scenario.Id, EntryType.Bill, "Spotify", 10m, 2,
            category: "Subscriptions", ai: "Subscriptions");

        Assert.True(corrected.UserCorrected);
        Assert.False(accepted.UserCorrected);
    }

    // ----- projection (the SRS demo) -----

    [Fact]
    public async Task Demo_flow_moving_the_rent_removes_the_warnings()
    {
        var c = fixture.CreateClient(Guid.NewGuid());
        var scenario = await CreateScenarioAsync(c, 1000m);
        var rent = await AddEntryAsync(c, scenario.Id, EntryType.Bill, "Rent", 4000m, 1, "Rent");
        await AddEntryAsync(c, scenario.Id, EntryType.Income, "Client payment", 6000m, 5, "Salary/Client Payment");

        var before = await GetProjectionAsync(c, scenario.Id);

        Assert.Equal(5, before.Days.Count);
        Assert.Equal(
            new[] { Start, Start.AddDays(1), Start.AddDays(2), Start.AddDays(3) },
            before.WarningDates);
        Assert.Equal(-3000m, before.Summary.LowestBalance);
        Assert.Equal(Start, before.Summary.LowestBalanceDate);
        Assert.Equal(3000m, before.Summary.EndingBalance);

        var move = await PatchAsync(c, $"/api/v1/entries/{rent.Id}", new { date = Start.AddDays(5) });
        Assert.Equal(HttpStatusCode.OK, move.StatusCode);

        var after = await GetProjectionAsync(c, scenario.Id);

        Assert.Empty(after.WarningDates);
        Assert.Equal(0, after.Summary.WarningDayCount);
        Assert.Equal(3000m, after.Summary.EndingBalance);
    }

    [Fact]
    public async Task Horizon_before_the_start_date_returns_400()
    {
        var c = fixture.CreateClient(Guid.NewGuid());
        var scenario = await CreateScenarioAsync(c);

        var r = await c.GetAsync($"/api/v1/scenarios/{scenario.Id}/projection?horizonEnd=2026-10-01");

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    // ----- samples -----

    [Theory]
    [InlineData("freelancer-shortfall", 4, -3000, 2100)]
    [InlineData("steady-month", 0, 700, 7700)]
    [InlineData("irregular-income", 11, -1700, 1700)]
    public async Task Sample_scenarios_produce_the_documented_projection(
        string key, int warningDays, int lowest, int ending)
    {
        var c = fixture.CreateClient(Guid.NewGuid());

        var r = await c.PostAsync($"/api/v1/samples/{key}", null);

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var scenario = await r.Content.ReadFromJsonAsync<ScenarioResponse>(Json);

        var projection = await GetProjectionAsync(c, scenario!.Id);
        Assert.Equal(warningDays, projection.Summary.WarningDayCount);
        Assert.Equal((decimal)lowest, projection.Summary.LowestBalance);
        Assert.Equal((decimal)ending, projection.Summary.EndingBalance);
    }

    [Fact]
    public async Task Unknown_sample_returns_404()
    {
        var c = fixture.CreateClient(Guid.NewGuid());

        var r = await c.PostAsync("/api/v1/samples/does-not-exist", null);

        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }
}