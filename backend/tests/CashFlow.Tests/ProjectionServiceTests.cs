using CashFlow.Core.Entities;
using CashFlow.Core.Projection;

namespace CashFlow.Tests;

public class ProjectionServiceTests
{
    private static readonly DateOnly Start = new(2026, 11, 1);
    private readonly ProjectionService _sut = new();

    private static DateOnly Day(int n) => Start.AddDays(n - 1);

    private static Entry Income(int day, decimal amount) => new()
    { Type = EntryType.Income, Description = "Income", Amount = amount, Date = Day(day) };

    private static Entry Bill(int day, decimal amount) => new()
    { Type = EntryType.Bill, Description = "Bill", Amount = amount, Date = Day(day) };

    private static Scenario MakeScenario(decimal startingBalance, params Entry[] entries) => new()
    { Name = "Test", StartingBalance = startingBalance, StartDate = Start, Entries = entries.ToList() };

    [Fact]
    public void Empty_scenario_returns_single_day_with_starting_balance()
    {
        var result = _sut.Calculate(MakeScenario(1000));

        var day = Assert.Single(result.Days);
        Assert.Equal(Start, day.Date);
        Assert.Equal(1000m, day.ClosingBalance);
        Assert.False(day.IsWarning);
    }

    [Fact]
    public void Balance_carries_forward_between_entries()
    {
        var result = _sut.Calculate(MakeScenario(1000, Income(2, 500), Bill(4, 300)));

        Assert.Equal(new[] { 1000m, 1500m, 1500m, 1200m },
            result.Days.Select(d => d.ClosingBalance));
    }

    [Fact]
    public void Same_day_entries_are_all_applied()
    {
        var result = _sut.Calculate(MakeScenario(0, Income(1, 100), Bill(1, 40), Bill(1, 10)));

        var day = Assert.Single(result.Days);
        Assert.Equal(50m, day.ClosingBalance);
        Assert.Equal(3, day.Entries.Count);
    }

    [Fact]
    public void Balance_goes_negative_then_recovers()
    {
        // Rent on day 1, client payment on day 5.
        var result = _sut.Calculate(MakeScenario(1000, Bill(1, 4000), Income(5, 6000)));

        Assert.Equal(new[] { 1, 2, 3, 4 }, result.WarningDays.Select(d => d.Date.Day));
        Assert.Equal(-3000m, result.LowestDay!.ClosingBalance);
        Assert.Equal(Day(1), result.LowestDay.Date);
        Assert.Equal(3000m, result.Days.Last().ClosingBalance);
    }

    [Fact]
    public void Moving_the_bill_later_removes_the_warnings()
    {
        // Same as above, but the rent moves from day 1 to day 6.
        var result = _sut.Calculate(MakeScenario(1000, Bill(6, 4000), Income(5, 6000)));

        Assert.Empty(result.WarningDays);
        Assert.Equal(3000m, result.Days.Last().ClosingBalance);
    }

    [Fact]
    public void Negative_starting_balance_is_flagged_from_day_one()
    {
        var result = _sut.Calculate(MakeScenario(-200, Income(3, 500)));

        Assert.Equal(new[] { Day(1), Day(2) }, result.WarningDays.Select(d => d.Date));
        Assert.Equal(300m, result.Days.Last().ClosingBalance);
    }

    [Fact]
    public void Entries_before_the_start_date_are_ignored()
    {
        var early = new Entry
        { Type = EntryType.Bill, Description = "Early", Amount = 999, Date = Start.AddDays(-1) };

        var result = _sut.Calculate(MakeScenario(100, early));

        var day = Assert.Single(result.Days);
        Assert.Equal(100m, day.ClosingBalance);
    }

    [Fact]
    public void Horizon_extends_the_projection_past_the_last_entry()
    {
        var result = _sut.Calculate(MakeScenario(100, Bill(2, 10)), Day(5));

        Assert.Equal(5, result.Days.Count);
        Assert.Equal(90m, result.Days.Last().ClosingBalance);
    }

    [Fact]
    public void Horizon_excludes_entries_after_it()
    {
        var result = _sut.Calculate(MakeScenario(100, Bill(2, 10), Bill(9, 50)), Day(5));

        Assert.Equal(5, result.Days.Count);
        Assert.Equal(90m, result.Days.Last().ClosingBalance);
    }

    [Fact]
    public void Horizon_longer_than_the_cap_throws()
    {
        var tooFar = Start.AddDays(ProjectionService.MaxHorizonDays + 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => _sut.Calculate(MakeScenario(0), tooFar));
    }

    [Fact]
    public void Horizon_before_the_start_date_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _sut.Calculate(MakeScenario(0), Start.AddDays(-1)));
    }
}