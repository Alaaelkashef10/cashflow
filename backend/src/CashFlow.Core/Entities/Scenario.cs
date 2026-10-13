namespace CashFlow.Core.Entities;

public class Scenario
{
    public int Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = "";
    public decimal StartingBalance { get; set; }
    public DateOnly StartDate { get; set; }
    public List<Entry> Entries { get; set; } = new();
}