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
/// nullable number, datetime, bool, enum, guid. The nullable enum and nullable Guid columns exist
/// so the generator's <c>MapNullable</c> lifting is exercised on non-numeric column types too.
/// Filter and sort scenarios run against the <c>Widgets</c> table on every configured provider.
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
    public WidgetStatus? OptionalStatus { get; set; }
    public Guid ExternalId { get; set; }
    public Guid? OptionalExternalId { get; set; }
}
