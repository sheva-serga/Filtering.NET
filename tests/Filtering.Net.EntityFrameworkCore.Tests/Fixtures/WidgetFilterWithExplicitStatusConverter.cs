namespace Filtering.Net.EntityFrameworkCore.Tests.Fixtures;

/// <summary>
/// Filter partial that exercises <c>[ConvertWith&lt;TConverter&gt;]</c> against the
/// custom <see cref="WidgetStatusConverter"/>. Maps only Id and Status — the rest of
/// the WidgetEntity surface is covered by <see cref="WidgetFilter"/> — so these
/// scenarios stay focused on the value-converter codepath.
/// </summary>
[GenerateFilter<WidgetEntity>]
public partial class WidgetFilterWithExplicitStatusConverter
{
    [Map(nameof(WidgetEntity.Id), Sortable = true)]
    private static partial void MapId();

    [Map(nameof(WidgetEntity.Status))]
    [ConvertWith<WidgetStatusConverter>]
    private static partial void MapStatus();
}
