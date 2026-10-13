using CashFlow.Core.Entities;

namespace CashFlow.Core.Projection;

public sealed record DayProjection(
    DateOnly Date,
    decimal ClosingBalance,
    bool IsWarning,
    IReadOnlyList<Entry> Entries);

public sealed record ProjectionResult(IReadOnlyList<DayProjection> Days)
{
    public IReadOnlyList<DayProjection> WarningDays =>
        Days.Where(d => d.IsWarning).ToList();

    public DayProjection? LowestDay =>
        Days.Count == 0 ? null : Days.MinBy(d => d.ClosingBalance);
}