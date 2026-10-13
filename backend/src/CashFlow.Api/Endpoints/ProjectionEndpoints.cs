using CashFlow.Api.Contracts;
using CashFlow.Core.Projection;
using CashFlow.Infrastructure.Persistence;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Api.Endpoints;

public static class ProjectionEndpoints
{
    public static RouteGroupBuilder MapProjectionEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/scenarios/{id:int}/projection", GetAsync).WithTags("Projection");
        return api;
    }

    private static async Task<Results<Ok<ProjectionResponse>, NotFound, ValidationProblem>> GetAsync(
        int id,
        DateOnly? horizonEnd,
        [FromHeader(Name = "X-Owner-Id")] Guid ownerId,
        AppDbContext db,
        IProjectionService projection,
        CancellationToken ct)
    {
        var scenario = await db.Scenarios
            .AsNoTracking()
            .Include(s => s.Entries)
            .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == ownerId, ct);

        if (scenario is null) return TypedResults.NotFound();

        ProjectionResult result;
        try
        {
            result = projection.Calculate(scenario, horizonEnd);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["horizonEnd"] = [ex.Message]
            });
        }

        var days = result.Days
            .Select(d => new ProjectionDayResponse(
                d.Date,
                d.ClosingBalance,
                d.IsWarning,
                d.Entries.Select(EntryMapper.ToResponse).ToList()))
            .ToList();

        var warnings = result.WarningDays;
        var lowest = result.LowestDay!;
        var last = days[^1];

        return TypedResults.Ok(new ProjectionResponse(
            scenario.Id,
            scenario.StartingBalance,
            scenario.StartDate,
            last.Date,
            days,
            warnings.Select(d => d.Date).ToList(),
            new ProjectionSummary(lowest.ClosingBalance, lowest.Date, warnings.Count, last.ClosingBalance)));
    }
}