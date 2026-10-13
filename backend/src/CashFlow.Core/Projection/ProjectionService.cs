using CashFlow.Core.Entities;

namespace CashFlow.Core.Projection;

public class ProjectionService : IProjectionService
{
    public const int MaxHorizonDays = 730;

    public ProjectionResult Calculate(Scenario scenario, DateOnly? horizonEnd = null)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        var start = scenario.StartDate;

        // Entries dated before the start date are ignored.
        var relevant = scenario.Entries.Where(e => e.Date >= start).ToList();

        var end = horizonEnd ?? (relevant.Count > 0 ? relevant.Max(e => e.Date) : start);

        if (end < start)
            throw new ArgumentOutOfRangeException(nameof(horizonEnd),
                "The end date cannot be before the scenario start date.");

        if (end.DayNumber - start.DayNumber > MaxHorizonDays)
            throw new ArgumentOutOfRangeException(nameof(horizonEnd),
                $"The projection cannot be longer than {MaxHorizonDays} days.");

        var byDate = relevant.Where(e => e.Date <= end).ToLookup(e => e.Date);

        var balance = scenario.StartingBalance;
        var days = new List<DayProjection>();

        for (var date = start; date <= end; date = date.AddDays(1))
        {
            var todays = byDate[date].ToList();

            foreach (var entry in todays)
                balance += entry.Type == EntryType.Income ? entry.Amount : -entry.Amount;

            days.Add(new DayProjection(date, balance, balance < 0, todays));
        }

        return new ProjectionResult(days);
    }
}