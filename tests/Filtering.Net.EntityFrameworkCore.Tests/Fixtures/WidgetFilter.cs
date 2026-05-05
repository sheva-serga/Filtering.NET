namespace Filtering.Net.EntityFrameworkCore.Tests.Fixtures;

/// <summary>
/// Generated <see cref="IFilterDefinition{TEntity}"/> for <see cref="WidgetEntity"/>. Each
/// <c>[Map]</c> method reaches one of the built-in primitive profiles by inference; the source
/// generator emits the implementation.
/// </summary>
[GenerateFilter<WidgetEntity>]
public partial class WidgetFilter
{
    [Map(nameof(WidgetEntity.Id), Sortable = true)]
    private static partial void MapId();

    [Map(nameof(WidgetEntity.Name), Sortable = true)]
    private static partial void MapName();

    [Map(nameof(WidgetEntity.Quantity), Sortable = true)]
    private static partial void MapQuantity();

    [Map(nameof(WidgetEntity.Price), Sortable = true)]
    private static partial void MapPrice();

    [Map(nameof(WidgetEntity.OptionalCount), Sortable = true)]
    private static partial void MapOptionalCount();

    [Map(nameof(WidgetEntity.CreatedAt), Sortable = true)]
    private static partial void MapCreatedAt();

    [Map(nameof(WidgetEntity.IsActive))]
    private static partial void MapIsActive();

    [Map(nameof(WidgetEntity.Status), Sortable = true)]
    private static partial void MapStatus();

    [Map(nameof(WidgetEntity.ExternalId))]
    private static partial void MapExternalId();
}
