namespace Filtering.Net.Generator;

internal sealed record PropertyValueShape(
    string LeafValueClrType,
    string ProfileFullName,
    bool IsNullableValueType);

internal static class PropertyValueShapeResolver
{
    public static PropertyValueShape Resolve(string propertyClrType, string profileFullName)
    {
        var trimmed = propertyClrType;
        var nullableValueType = false;
        if (trimmed.EndsWith("?", StringComparison.Ordinal))
        {
            trimmed = trimmed.Substring(0, trimmed.Length - 1);
            nullableValueType = true;
        }
        var leafType = MapLeafType(trimmed);
        return new PropertyValueShape(leafType, profileFullName, nullableValueType);
    }

    private static string MapLeafType(string trimmed) => trimmed switch
    {
        "string" or "System.String" => "string",
        "int" or "System.Int32" => "int",
        "long" or "System.Int64" => "long",
        "short" or "System.Int16" => "short",
        "byte" or "System.Byte" => "byte",
        "decimal" or "System.Decimal" => "decimal",
        "double" or "System.Double" => "double",
        "float" or "System.Single" => "float",
        "bool" or "System.Boolean" => "bool",
        "System.Guid" => "global::System.Guid",
        "System.DateTime" => "global::System.DateTime",
        "System.DateTimeOffset" => "global::System.DateTimeOffset",
        "System.DateOnly" => "global::System.DateOnly",
        "System.TimeOnly" => "global::System.TimeOnly",
        _ => "global::" + trimmed,
    };
}
