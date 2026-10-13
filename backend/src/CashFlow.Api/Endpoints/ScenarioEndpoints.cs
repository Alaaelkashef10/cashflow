using CashFlow.Api.Contracts;
using CashFlow.Api.Validation;
using CashFlow.Core.Entities;
using CashFlow.Infrastructure.Persistence;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Api.Endpoints;

public static class ScenarioEndpoints
{
    private const string OwnerHeader = "X-Owner-Id";

    public static RouteGroupBuilder MapScenarioEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/scenarios").WithTags("Scenarios");

        group.MapGet("/", ListAsync);
        group.MapGet("/{id:int}", GetAsync);
        group.MapPost("/", CreateAsync).WithValidation<CreateScenarioRequest>();
        group.MapPatch("/{id:int}", UpdateAsync).WithValidation<UpdateScenarioRequest>();
        group.MapDelete("/{id:int}", DeleteAsync);

        return api;
    }

    private static async Task<Ok<List<ScenarioResponse>>> ListAsync(
        [FromHeader(Name = OwnerHeader)] Guid ownerId,
        AppDbContext db,
        CancellationToken ct)
    {
        var items = await db.Scenarios
            .AsNoTracking()
            .Where(s => s.OwnerId == ownerId)
            .OrderByDescending(s => s.Id)
            .Select(s => new ScenarioResponse(s.Id, s.Name, s.StartingBalance, s.StartDate, s.Entries.Count))
            .ToListAsync(ct);

        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<ScenarioResponse>, NotFound>> GetAsync(
        int id,
        [FromHeader(Name = OwnerHeader)] Guid ownerId,
        AppDbContext db,
        CancellationToken ct)
    {
        var item = await db.Scenarios
            .AsNoTracking()
            .Where(s => s.Id == id && s.OwnerId == ownerId)
            .Select(s => new ScenarioResponse(s.Id, s.Name, s.StartingBalance, s.StartDate, s.Entries.Count))
            .FirstOrDefaultAsync(ct);

        if (item is null) return TypedResults.NotFound();
        return TypedResults.Ok(item);
    }

    private static async Task<Created<ScenarioResponse>> CreateAsync(
        [FromHeader(Name = OwnerHeader)] Guid ownerId,
        CreateScenarioRequest request,
        AppDbContext db,
        CancellationToken ct)
    {
        var scenario = new Scenario
        {
            OwnerId = ownerId,
            Name = request.Name.Trim(),
            StartingBalance = request.StartingBalance,
            StartDate = request.StartDate
        };

        db.Scenarios.Add(scenario);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/api/v1/scenarios/{scenario.Id}", ToResponse(scenario, 0));
    }

    private static async Task<Results<Ok<ScenarioResponse>, NotFound>> UpdateAsync(
        int id,
        [FromHeader(Name = OwnerHeader)] Guid ownerId,
        UpdateScenarioRequest request,
        AppDbContext db,
        CancellationToken ct)
    {
        var scenario = await db.Scenarios
            .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == ownerId, ct);

        if (scenario is null) return TypedResults.NotFound();

        if (request.Name is not null) scenario.Name = request.Name.Trim();
        if (request.StartingBalance.HasValue) scenario.StartingBalance = request.StartingBalance.Value;
        if (request.StartDate.HasValue) scenario.StartDate = request.StartDate.Value;

        await db.SaveChangesAsync(ct);

        var entryCount = await db.Entries.CountAsync(e => e.ScenarioId == id, ct);
        return TypedResults.Ok(ToResponse(scenario, entryCount));
    }

    private static async Task<Results<NoContent, NotFound>> DeleteAsync(
        int id,
        [FromHeader(Name = OwnerHeader)] Guid ownerId,
        AppDbContext db,
        CancellationToken ct)
    {
        // Entries are removed by the database (ON DELETE CASCADE).
        var deleted = await db.Scenarios
            .Where(s => s.Id == id && s.OwnerId == ownerId)
            .ExecuteDeleteAsync(ct);

        if (deleted == 0) return TypedResults.NotFound();
        return TypedResults.NoContent();
    }

    private static ScenarioResponse ToResponse(Scenario s, int entryCount) =>
        new(s.Id, s.Name, s.StartingBalance, s.StartDate, entryCount);
}