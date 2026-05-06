using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Filtering.Net.EntityFrameworkCore.Tests.Fixtures;

public sealed class WidgetStatusConverter : ValueConverter<WidgetStatus, string>
{
    public WidgetStatusConverter()
        : base(
            status => status.ToString(),
            name => Enum.Parse<WidgetStatus>(name))
    {
    }
}
