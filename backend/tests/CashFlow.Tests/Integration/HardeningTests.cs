using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;

using CashFlow.Api.Contracts;
using CashFlow.Core;
using CashFlow.Core.Entities;
using CashFlow.Infrastructure.Persistence;

using Microsoft.Extensions.DependencyInjection;

namespace CashFlow.Tests.Integration;

[Collection("api")]
public class HardeningTests(ApiFixture fixture)
{
    private static readonly DateOnly Start = new(2026, 11, 1);

    [Fact]
    public async Task Responses_carry_security_headers()
    {
        var response = await fixture.CreateClient().GetAsync("/health");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
    }

    [Fact]
    public async Task Owner_cannot_exceed_the_scenario_limit()
    {
        var owner = Guid.NewGuid();
        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Scenarios.AddRange(Enumerable.Range(1, Limits.MaxScenariosPerOwner)
                .Select(i => new Scenario { OwnerId = owner, Name = $"S{i}", StartDate = Start }));
            await db.SaveChangesAsync();
        }
        var client = fixture.CreateClient(owner);

        var create = await client.PostAsJsonAsync("/api/v1/scenarios",
            new CreateScenarioRequest("One too many", 0m, Start), ApiFixture.Json);
        var sample = await client.PostAsync("/api/v1/samples/steady-month", null);

        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
        Assert.Contains("at most 50 scenarios", await create.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, sample.StatusCode);
    }

    [Fact]
    public async Task Scenario_cannot_exceed_the_entry_limit()
    {
        var owner = Guid.NewGuid();
        var client = fixture.CreateClient(owner);

        var created = await client.PostAsJsonAsync("/api/v1/scenarios",
            new CreateScenarioRequest("Full", 0m, Start), ApiFixture.Json);
        var scenario = (await created.Content.ReadFromJsonAsync<ScenarioResponse>(ApiFixture.Json))!;

        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Entries.AddRange(Enumerable.Range(0, Limits.MaxEntriesPerScenario).Select(i => new Entry
            {
                ScenarioId = scenario.Id,
                Type = EntryType.Bill,
                Description = $"E{i}",
                Amount = 1m,
                Date = Start.AddDays(i % 365),
                Category = "Other Expense"
            }));
            await db.SaveChangesAsync();
        }

        var response = await client.PostAsJsonAsync($"/api/v1/scenarios/{scenario.Id}/entries",
            new CreateEntryRequest(EntryType.Bill, "One too many", 1m, Start, "Other Expense", null),
            ApiFixture.Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("at most 500 entries", await response.Content.ReadAsStringAsync());
    }

    // NFR-01: a projection of up to 500 entries returns within 1 second.
    [Fact]
    public async Task Projection_of_500_entries_returns_within_one_second()
    {
        var owner = Guid.NewGuid();
        int scenarioId;

        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var scenario = new Scenario
            {
                OwnerId = owner,
                Name = "Big",
                StartingBalance = 10_000m,
                StartDate = Start,
                Entries = Enumerable.Range(0, Limits.MaxEntriesPerScenario).Select(i => new Entry
                {
                    Type = i % 5 == 0 ? EntryType.Income : EntryType.Bill,
                    Description = $"E{i}",
                    Amount = 100m + i,
                    Date = Start.AddDays(i * 7 % 700),
                    Category = "Other Expense"
                }).ToList()
            };
            db.Scenarios.Add(scenario);
            await db.SaveChangesAsync();
            scenarioId = scenario.Id;
        }

        var client = fixture.CreateClient(owner);
        var url = $"/api/v1/scenarios/{scenarioId}/projection";

        await client.GetAsync(url); // warm-up (JIT and connection pool), not measured

        var timer = Stopwatch.StartNew();
        var response = await client.GetAsync(url);
        timer.Stop();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<ProjectionResponse>(ApiFixture.Json))!;
        Assert.True(body.Days.Count >= 600);
        Assert.True(timer.ElapsedMilliseconds < 1000, $"Projection took {timer.ElapsedMilliseconds} ms.");
    }
}