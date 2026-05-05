using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Filtering.Net.EntityFrameworkCore.Tests.Fixtures;

/// <summary>Explicit <see cref="ValueConverter{TModel, TProvider}"/> mapping
/// <see cref="WidgetStatus"/> to its enum-name string. Used both by
/// <see cref="ScenarioDbContext.OnModelCreating"/> on the EF model side and by
/// <c>[ConvertWith&lt;WidgetStatusConverter&gt;]</c> on the filter side, so the
/// integration scenarios prove the converter round-trips end to end.</summary>
public sealed class WidgetStatusConverter : ValueConverter<WidgetStatus, string>
{
    public WidgetStatusConverter()
        : base(
            status => status.ToString(),
            name => Enum.Parse<WidgetStatus>(name))
    {
    }
}
