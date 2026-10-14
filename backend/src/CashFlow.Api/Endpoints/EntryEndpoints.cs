using CashFlow.Api.Contracts;
using CashFlow.Api.Validation;
using CashFlow.Core;
using CashFlow.Core.Entities;
using CashFlow.Infrastructure.Persistence;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Api.Endpoints;

public static class EntryEndpoints
{
    private const string OwnerHeader = "X-Owner-Id";

    public static RouteGroupBuilder MapEntryEndpoints(this RouteGroupBuilder api)
    {
        var inScenario = api.MapGroup("/scenarios/{scenarioId:int}/entries").WithTags("Entries");
        inScenario.MapGet("/", ListAsync);
        inScenario.MapPost("/", CreateAsync).WithValidation<CreateEntryRequest>();

        var entries = api.MapGroup("/entries").WithTags("Entries");
        entries.MapPatch("/{id:int}", UpdateAsync).WithValidation<UpdateEntryRequest>();
        entries.MapDelete("/{id:int}", DeleteAsync);

        return api;
    }

    private static async Task<Results<Ok<List<EntryResponse>>, NotFound>> ListAsync(
        int scenarioId,
        [FromHeader(Name = OwnerHeader)] Guid ownerId,
        AppDbContext db,
        CancellationToken ct)
    {
        var owns = await db.Scenarios.AnyAsync(s => s.Id == scenarioId && s.OwnerId == ownerId, ct);
        if (!owns) return TypedResults.NotFound();

        var items = await db.Entries
            .AsNoTracking()
            .Where(e => e.ScenarioId == scenarioId)
            .OrderBy(e => e.Date).ThenBy(e => e.Id)
            .ToListAsync(ct);

        return TypedResults.Ok(items.Select(EntryMapper.ToResponse).ToList());
    }

    private static async Task<Results<Created<EntryResponse>, NotFound, ValidationProblem>> CreateAsync(
        int scenarioId,
        [FromHeader(Name = OwnerHeader)] Guid ownerId,
        CreateEntryRequest request,
        AppDbContext db,
        CancellationToken ct)
    {
        var owns = await db.Scenarios.AnyAsync(s => s.Id == scenarioId && s.OwnerId == ownerId, ct);
        if (!owns) return TypedResults.NotFound();

        var count = await db.Entries.CountAsync(e => e.ScenarioId == scenarioId, ct);
        if (count >= Limits.MaxEntriesPerScenario)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [""] = [$"A scenario can have at most {Limits.MaxEntriesPerScenario} entries."]
            });
        }

        var entry = new Entry
        {
            ScenarioId = scenarioId,
            Type = request.Type,
            Description = request.Description.Trim(),
            Amount = request.Amount,
            Date = request.Date,
            Category = request.Category ?? Categories.Uncategorized,
            AiSuggestedCategory = request.AiSuggestedCategory
        };
        entry.UserCorrected = entry.AiSuggestedCategory is not null
                              && entry.AiSuggestedCategory != entry.Category;

        db.Entries.Add(entry);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created(
            $"/api/v1/scenarios/{scenarioId}/entries", EntryMapper.ToResponse(entry));
    }

    private static async Task<Results<Ok<EntryResponse>, NotFound>> UpdateAsync(
        int id,
        [FromHeader(Name = OwnerHeader)] Guid ownerId,
        UpdateEntryRequest request,
        AppDbContext db,
        CancellationToken ct)
    {
        var entry = await db.Entries.FirstOrDefaultAsync(
            e => e.Id == id && db.Scenarios.Any(s => s.Id == e.ScenarioId && s.OwnerId == ownerId), ct);

        if (entry is null) return TypedResults.NotFound();

        if (request.Type.HasValue) entry.Type = request.Type.Value;
        if (request.Description is not null) entry.Description = request.Description.Trim();
        if (request.Amount.HasValue) entry.Amount = request.Amount.Value;
        if (request.Date.HasValue) entry.Date = request.Date.Value;

        if (request.Category is not null)
        {
            entry.Category = request.Category;
            entry.UserCorrected = entry.AiSuggestedCategory is not null
                                  && entry.AiSuggestedCategory != entry.Category;
        }

        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(EntryMapper.ToResponse(entry));
    }

    private static async Task<Results<NoContent, NotFound>> DeleteAsync(
        int id,
        [FromHeader(Name = OwnerHeader)] Guid ownerId,
        AppDbContext db,
        CancellationToken ct)
    {
        var deleted = await db.Entries
            .Where(e => e.Id == id && db.Scenarios.Any(s => s.Id == e.ScenarioId && s.OwnerId == ownerId))
            .ExecuteDeleteAsync(ct);

        if (deleted == 0) return TypedResults.NotFound();
        return TypedResults.NoContent();
    }
}