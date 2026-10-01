namespace Filtering.Net.EntityFrameworkCore.Tests.Fixtures;

[GenerateFilter<WidgetEntity>]
[Map(nameof(WidgetEntity.Id), Sortable = true)]
public partial class WidgetComputedSortFilter
{
    [PropertyMap("OptionalCountOrZero", Alias = "countOrZero", Sortable = true)]
    private static FilterRule<WidgetEntity, int> MapOptionalCountOrZero(FilterRuleBuilder<WidgetEntity, int> builder) =>
        builder.For(widget => widget.OptionalCount ?? 0);
}
