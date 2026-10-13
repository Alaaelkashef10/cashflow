namespace CashFlow.Core.Entities;

public static class Categories
{
    public const string Uncategorized = "Uncategorized";

    public static readonly IReadOnlyList<string> All =
    [
        "Rent",
        "Utilities",
        "Subscriptions",
        "Groceries",
        "Transport",
        "Loans/Installments",
        "Salary/Client Payment",
        "Other Income",
        "Other Expense",
        Uncategorized
    ];

    public static bool IsValid(string? category) =>
        category is not null && All.Contains(category, StringComparer.Ordinal);
}