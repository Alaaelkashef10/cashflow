using CashFlow.Api.Contracts;
using CashFlow.Core.Samples;
using CashFlow.Infrastructure.Persistence;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Api.Endpoints;

public static class SampleEndpoints
{
    public static RouteGroupBuilder MapSampleEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/samples").WithTags("Samples");
        group.MapGet("/", List);
        group.MapPost("/{key}", CreateAsync);
        return api;
    }

    private static Ok<List<SampleInfo>> List() =>
        TypedResults.Ok(SampleCatalog.All
            .Select(s => new SampleInfo(s.Key, s.Name, s.Description))
            .ToList());

    private static async Task<Results<Created<ScenarioResponse>, NotFound>> CreateAsync(
        string key,
        [FromHeader(Name = "X-Owner-Id")] Guid ownerId,
        AppDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var sample = SampleCatalog.Find(key);
        if (sample is null) return TypedResults.NotFound();

        // Samples start on the first day of next month.
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        var start = new DateOnly(today.Year, today.Month, 1).AddMonths(1);

        var scenario = sample.ToScenario(ownerId, start);
        db.Scenarios.Add(scenario);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created(
            $"/api/v1/scenarios/{scenario.Id}",
            new ScenarioResponse(
                scenario.Id, scenario.Name, scenario.StartingBalance,
                scenario.StartDate, scenario.Entries.Count));
    }
}