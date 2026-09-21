using Microsoft.EntityFrameworkCore;

namespace Filtering.Net.EntityFrameworkCore.Tests.Fixtures;

public sealed class ScenarioDbContext(DbContextOptions<ScenarioDbContext> options) : DbContext(options)
{
    public DbSet<WidgetEntity> Widgets => Set<WidgetEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WidgetEntity>().HasKey(widget => widget.Id);
        // WidgetSeed assigns the keys; SQL Server rejects explicit values for an identity column.
        modelBuilder.Entity<WidgetEntity>().Property(widget => widget.Id).ValueGeneratedNever();
        modelBuilder.Entity<WidgetEntity>()
            .Property(widget => widget.Status)
            .HasConversion<WidgetStatusConverter>();
    }
}
