namespace Filtering.Net.EntityFrameworkCore.Tests.Fixtures;

/// <summary>Seeds a deterministic, hand-crafted set of <see cref="WidgetEntity"/> rows used by
/// the scenario tests. All fields cover meaningful boundary values for the filter operators.</summary>
internal static class WidgetSeed
{
    public static async Task SeedAsync(ScenarioDbContext dbContext)
    {
        if (await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(dbContext.Widgets))
            return;
        var baseDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        dbContext.Widgets.AddRange(
            new WidgetEntity { Id = 1, Name = "Alpha", Quantity = 10, Price = 9.99m, OptionalCount = 1, CreatedAt = baseDate.AddDays(0), IsActive = true, Status = WidgetStatus.Active, ExternalId = new Guid("11111111-1111-1111-1111-111111111111") },
            new WidgetEntity { Id = 2, Name = "Beta",  Quantity = 20, Price = 19.50m, OptionalCount = null, CreatedAt = baseDate.AddDays(1), IsActive = false, Status = WidgetStatus.Pending, ExternalId = new Guid("22222222-2222-2222-2222-222222222222") },
            new WidgetEntity { Id = 3, Name = "Gamma", Quantity = 30, Price = 29.00m, OptionalCount = 5, CreatedAt = baseDate.AddDays(2), IsActive = true, Status = WidgetStatus.Archived, ExternalId = new Guid("33333333-3333-3333-3333-333333333333") },
            new WidgetEntity { Id = 4, Name = "Delta", Quantity = 40, Price = 39.99m, OptionalCount = null, CreatedAt = baseDate.AddDays(3), IsActive = true, Status = WidgetStatus.Active, ExternalId = new Guid("44444444-4444-4444-4444-444444444444") },
            new WidgetEntity { Id = 5, Name = "Epsilon", Quantity = 50, Price = 49.95m, OptionalCount = 8, CreatedAt = baseDate.AddDays(4), IsActive = false, Status = WidgetStatus.Pending, ExternalId = new Guid("55555555-5555-5555-5555-555555555555") }
        );
        await dbContext.SaveChangesAsync();
    }
}
