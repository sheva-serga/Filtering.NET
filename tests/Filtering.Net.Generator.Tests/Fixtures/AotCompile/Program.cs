// This fixture exists solely to verify that the source generator's emitted code
// does not introduce IL2026 (RequiresUnreferencedCode) or IL3050 (RequiresDynamicCode)
// warnings when the consuming project is built with PublishAot=true.

using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Filtering.Net;

namespace AotFixture;

// A custom value type for a typed-value operator — forces the generator to emit
// JsonSerializer.Deserialize<T>(json, typeInfo) in the Build/Validate arms.
public sealed record PrefixFilterValue(string Prefix);

[FilterProfile<string>(BasedOn = typeof(StringFilter))]
public static class StringWithPrefixProfile
{
    [FilterOperator("prefixMatch")]
    public static Expression<Func<string, PrefixFilterValue, bool>> PrefixMatch =>
        (column, value) => column.StartsWith(value.Prefix);
}

public sealed class User
{
    public string Email { get; set; } = string.Empty;
}

[GenerateFilter<User>]
public partial class UserFilter
{
    [Map(nameof(User.Email), Profile = typeof(StringWithPrefixProfile), Only = new[] { "prefixMatch" })]
    private static partial void MapEmail();
}

[JsonSerializable(typeof(PrefixFilterValue))]
internal partial class AotJsonContext : JsonSerializerContext;

public static class Program
{
    public static int Main()
    {
        var filter = new UserFilter(AotJsonContext.Default);
        return 0;
    }
}
