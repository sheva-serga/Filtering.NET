using Microsoft.EntityFrameworkCore;

namespace Filtering.Net.EntityFrameworkCore.Tests.Fixtures;

/// <summary>EF Core context for the scenario tests. Holds a single
/// <see cref="WidgetEntity"/> table.</summary>
public sealed class ScenarioDbContext(DbContextOptions<ScenarioDbContext> options) : DbContext(options)
{
    public DbSet<WidgetEntity> Widgets => Set<WidgetEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WidgetEntity>().HasKey(widget => widget.Id);
        modelBuilder.Entity<WidgetEntity>()
            .Property(widget => widget.Status)
            .HasConversion<string>(); // store enum as text so SQLite can sort/filter naturally
    }
}
