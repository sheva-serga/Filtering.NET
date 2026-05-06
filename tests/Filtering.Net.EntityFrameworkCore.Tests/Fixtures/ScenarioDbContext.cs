using Microsoft.EntityFrameworkCore;

namespace Filtering.Net.EntityFrameworkCore.Tests.Fixtures;

public sealed class ScenarioDbContext(DbContextOptions<ScenarioDbContext> options) : DbContext(options)
{
    public DbSet<WidgetEntity> Widgets => Set<WidgetEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WidgetEntity>().HasKey(widget => widget.Id);
        modelBuilder.Entity<WidgetEntity>()
            .Property(widget => widget.Status)
            .HasConversion<WidgetStatusConverter>();
    }
}
