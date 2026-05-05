namespace Filtering.Net.Generator;

/// <summary>
/// Computed per-property shape used during emission: the CLR type the JSON value gets
/// extracted into (<see cref="LeafValueClrType"/>), the full name of the resolved profile
/// class whose <c>TryGetValue</c>/<c>TryGetArray</c> helpers we'll call, and a flag for
/// nullable value types.
/// </summary>
internal sealed record PropertyValueShape(
    string LeafValueClrType,
    string ProfileFullName,
    bool IsNullableValueType);

/// <summary>
/// Maps a property's display-string CLR type to its <see cref="PropertyValueShape"/>. Centralises
/// the brittle string-matching logic so the rest of the emitter can rely on a typed model.
/// </summary>
internal static class PropertyValueShapeResolver
{
    /// <summary>Computes the value shape for a given CLR display-string type and the
    /// resolved profile's full name. Strips a single trailing <c>?</c> for nullable annotation
    /// and remembers whether the original type was nullable so leaf emission can emit nullable
    /// accessors where needed. The leaf CLR type is mapped through <see cref="MapLeafType"/> so
    /// the rest of the emitter never has to care whether it's looking at a primitive, a
    /// well-known framework struct, or a custom (e.g., enum) type.</summary>
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

    /// <summary>Translates a (de-nullable-ed) CLR display string into the form the emitter
    /// uses inside generated method signatures and casts. Built-in primitives map to their
    /// C# keyword form; framework value types are fully qualified with <c>global::</c>;
    /// anything else (custom enums, user types) falls into the default branch and is also
    /// fully qualified.</summary>
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
