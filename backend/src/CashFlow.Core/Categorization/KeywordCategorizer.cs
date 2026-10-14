using System.Text.RegularExpressions;

using CashFlow.Core.Entities;

namespace CashFlow.Core.Categorization;

public sealed class KeywordCategorizer : ICategorizer
{
    public const string SourceName = "keyword";

    // How the rules work:
    // - A keyword matches at the start of a word ("rent" also matches "rental").
    // - A keyword ending in "$" must match a whole word ("bus$" does not match "business").
    // - Each matched keyword scores its length, so longer, more specific keywords win.
    // - On a tie, the category listed first wins.
    private static readonly (string Category, string[] Keywords)[] Rules =
    [
        ("Loans/Installments", new[] { "loan", "installment", "instalment", "mortgage", "credit card", "emi$", "repayment", "debt", "valu$", "bnpl" }),
        ("Subscriptions", new[] { "subscription", "netflix", "spotify", "youtube premium", "disney", "hbo", "prime video", "apple music", "icloud", "google one", "chatgpt", "streaming", "anghami", "shahid", "adobe", "microsoft 365", "office 365", "dropbox", "patreon", "gym", "membership", "game pass", "playstation plus" }),
        ("Rent", new[] { "rent", "landlord", "lease", "apartment", "housing", "tenancy" }),
        ("Utilities", new[] { "electric", "power bill", "water", "gas$", "internet", "wifi", "wi-fi", "broadband", "phone bill", "mobile bill", "utilit", "sewage", "trash", "garbage", "heating" }),
        ("Transport", new[] { "uber", "careem", "taxi", "cab$", "bus$", "metro", "train$", "fuel", "petrol", "gasoline", "diesel", "parking", "toll$", "fare", "flight", "airline", "ride$" }),
        ("Groceries", new[] { "grocer", "supermarket", "hypermarket", "carrefour", "spinneys", "metro market", "kazyon", "hyper one", "walmart", "aldi", "costco", "tesco", "vegetable", "fruit", "bakery", "butcher", "milk", "eggs$", "egg$", "bread", "food shopping" }),
        ("Salary/Client Payment", new[] { "salary", "paycheck", "payroll", "wage", "client", "invoice", "freelance", "consulting", "consultancy", "commission", "retainer", "upwork", "fiverr", "milestone", "bonus", "project payment", "contract payment" }),
        ("Other Income", new[] { "refund", "cashback", "gift from", "dividend", "interest earned", "sold$", "resale", "rental income", "reimbursement", "prize", "winnings", "pocket money" }),
        ("Other Expense", new[] { "haircut", "barber", "salon", "pharmacy", "doctor", "clinic", "hospital", "dentist", "medicine", "restaurant", "cafe", "coffee", "lunch", "dinner", "breakfast", "takeout", "talabat", "cinema", "movie", "clothes", "shoes", "repair", "plumber", "electrician", "gift for", "birthday", "donation", "charity", "tuition", "laundry", "pet$", "vet$", "wedding", "school fees", "insurance" }),
    ];

    private sealed record CompiledRule(string Category, (Regex Pattern, int Weight)[] Keywords);

    private static readonly CompiledRule[] Compiled =
        Rules.Select(r => new CompiledRule(r.Category, r.Keywords.Select(BuildKeyword).ToArray())).ToArray();

    private static (Regex, int) BuildKeyword(string keyword)
    {
        var exact = keyword.EndsWith('$');
        var text = exact ? keyword[..^1] : keyword;
        var body = Regex.Escape(text).Replace(@"\ ", @"\s+");
        var pattern = exact ? $@"\b{body}\b" : $@"\b{body}";
        var regex = new Regex(pattern,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        return (regex, text.Length);
    }

    public Task<CategorySuggestion> SuggestAsync(string description, CancellationToken ct = default)
        => Task.FromResult(Suggest(description));

    public CategorySuggestion Suggest(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return new CategorySuggestion(Categories.Uncategorized, 0, SourceName);

        var scored = Compiled
            .Select((rule, index) => (rule.Category, Index: index, Score: Score(rule, description)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Index)
            .ToList();

        if (scored.Count == 0)
            return new CategorySuggestion(Categories.Uncategorized, 0, SourceName);

        var top = scored[0].Score;
        var second = scored.Count > 1 ? scored[1].Score : 0;
        var confidence = second == 0 ? 0.9 : Math.Round(top / (double)(top + second), 2);

        return new CategorySuggestion(scored[0].Category, confidence, SourceName);
    }

    private static int Score(CompiledRule rule, string text) =>
        rule.Keywords.Where(k => k.Pattern.IsMatch(text)).Sum(k => k.Weight);
}