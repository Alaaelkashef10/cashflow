using CashFlow.Core.Entities;

namespace CashFlow.Core.Samples;

public sealed record SampleEntry(int Day, EntryType Type, string Description, decimal Amount, string Category);

public sealed record SampleDefinition(
    string Key,
    string Name,
    string Description,
    decimal StartingBalance,
    IReadOnlyList<SampleEntry> Entries)
{
    public Scenario ToScenario(Guid ownerId, DateOnly startDate) => new()
    {
        OwnerId = ownerId,
        Name = Name,
        StartingBalance = StartingBalance,
        StartDate = startDate,
        Entries = Entries.Select(e => new Entry
        {
            Type = e.Type,
            Description = e.Description,
            Amount = e.Amount,
            Date = startDate.AddDays(e.Day - 1),
            Category = e.Category
        }).ToList()
    };
}

public static class SampleCatalog
{
    public static readonly IReadOnlyList<SampleDefinition> All =
    [
        new("freelancer-shortfall",
            "Freelancer: rent before payday",
            "Rent is due on day 1 but the client pays on day 5, so the balance dips below zero for four days.",
            1000m,
            [
                new(1, EntryType.Bill, "Rent", 4000m, "Rent"),
                new(5, EntryType.Income, "Client payment", 6000m, "Salary/Client Payment"),
                new(8, EntryType.Bill, "Internet", 300m, "Utilities"),
                new(10, EntryType.Bill, "Groceries", 600m, "Groceries")
            ]),

        new("steady-month",
            "Steady month",
            "Income arrives late in the month, but the balance never goes below zero.",
            5000m,
            [
                new(3, EntryType.Bill, "Rent", 3000m, "Rent"),
                new(10, EntryType.Bill, "Electricity", 500m, "Utilities"),
                new(12, EntryType.Bill, "Groceries", 800m, "Groceries"),
                new(25, EntryType.Income, "Salary", 7000m, "Salary/Client Payment")
            ]),

        new("irregular-income",
            "Irregular income: two dips",
            "Two separate stretches below zero, because client payments arrive after the bills.",
            500m,
            [
                new(1, EntryType.Bill, "Rent", 2000m, "Rent"),
                new(2, EntryType.Bill, "Streaming subscription", 200m, "Subscriptions"),
                new(7, EntryType.Income, "Client A payment", 3000m, "Salary/Client Payment"),
                new(15, EntryType.Bill, "Loan installment", 1800m, "Loans/Installments"),
                new(20, EntryType.Income, "Client B payment", 2200m, "Salary/Client Payment")
            ])
    ];

    public static SampleDefinition? Find(string key) =>
        All.FirstOrDefault(s => s.Key == key);
}