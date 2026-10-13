using CashFlow.Core.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashFlow.Infrastructure.Persistence;

public class ScenarioConfiguration : IEntityTypeConfiguration<Scenario>
{
    public void Configure(EntityTypeBuilder<Scenario> builder)
    {
        builder.ToTable("Scenarios");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.Property(s => s.StartingBalance).HasPrecision(18, 2);

        builder.HasIndex(s => s.OwnerId);

        builder.HasMany(s => s.Entries)
            .WithOne()
            .HasForeignKey(e => e.ScenarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}