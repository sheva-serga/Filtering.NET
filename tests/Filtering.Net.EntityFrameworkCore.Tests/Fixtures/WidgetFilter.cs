namespace Filtering.Net.EntityFrameworkCore.Tests.Fixtures;

/// <summary>
/// Generated <see cref="IFilterDefinition{TEntity}"/> for <see cref="WidgetEntity"/>. Each
/// <c>[Map]</c> method reaches one of the built-in primitive profiles by inference; the source
/// generator emits the implementation.
/// </summary>
[GenerateFilter<WidgetEntity>]
[Map(nameof(WidgetEntity.Id), Sortable = true)]
[Map(nameof(WidgetEntity.Name), Sortable = true)]
[Map(nameof(WidgetEntity.Quantity), Sortable = true)]
[Map(nameof(WidgetEntity.Price), Sortable = true)]
[Map(nameof(WidgetEntity.OptionalCount), Sortable = true)]
[Map(nameof(WidgetEntity.CreatedAt), Sortable = true)]
[Map(nameof(WidgetEntity.IsActive))]
[Map(nameof(WidgetEntity.Status), Sortable = true)]
[Map(nameof(WidgetEntity.ExternalId))]
public partial class WidgetFilter;
