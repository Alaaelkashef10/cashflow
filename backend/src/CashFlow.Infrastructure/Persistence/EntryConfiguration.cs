using CashFlow.Core.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashFlow.Infrastructure.Persistence;

public class EntryConfiguration : IEntityTypeConfiguration<Entry>
{
    public void Configure(EntityTypeBuilder<Entry> builder)
    {
        builder.ToTable("Entries");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(10);
        builder.Property(e => e.Description).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.Category).IsRequired().HasMaxLength(50);
        builder.Property(e => e.AiSuggestedCategory).HasMaxLength(50);

        builder.HasIndex(e => new { e.ScenarioId, e.Date });
    }
}