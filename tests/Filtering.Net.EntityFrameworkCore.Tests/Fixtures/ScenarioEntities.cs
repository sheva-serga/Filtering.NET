namespace Filtering.Net.EntityFrameworkCore.Tests.Fixtures;

/// <summary>Status enum used by <see cref="WidgetEntity"/> for enum-profile scenarios.</summary>
public enum WidgetStatus
{
    Pending,
    Active,
    Archived,
}

/// <summary>
/// Test entity covering every primitive profile we care about: string, number (int/decimal),
/// nullable number, datetime, bool, enum, guid. Filter and sort scenarios are exercised against
/// the <c>Widgets</c> table backed by SQLite in-memory.
/// </summary>
public sealed class WidgetEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public int? OptionalCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public WidgetStatus Status { get; set; }
    public Guid ExternalId { get; set; }
}
