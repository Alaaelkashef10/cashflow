namespace CashFlow.Core.Entities;

public class Entry
{
    public int Id { get; set; }
    public int ScenarioId { get; set; }
    public EntryType Type { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }          // always positive; Type decides the sign
    public DateOnly Date { get; set; }
    public string Category { get; set; } = "Uncategorized";
    public string? AiSuggestedCategory { get; set; }
    public bool UserCorrected { get; set; }
}