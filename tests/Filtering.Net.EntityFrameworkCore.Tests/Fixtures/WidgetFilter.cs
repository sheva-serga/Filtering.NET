namespace Filtering.Net.EntityFrameworkCore.Tests.Fixtures;

/// <summary>
/// Generated <see cref="IFilterDefinition{TEntity}"/> for <see cref="WidgetEntity"/>. Each
/// class-level <c>[Map]</c> names one property; the profile is inferred from its CLR type. The
/// source generator emits the implementation.
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
[Map(nameof(WidgetEntity.OptionalStatus))]
[Map(nameof(WidgetEntity.ExternalId))]
[Map(nameof(WidgetEntity.OptionalExternalId))]
public partial class WidgetFilter;
