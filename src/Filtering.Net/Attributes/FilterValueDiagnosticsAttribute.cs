namespace Filtering.Net;

/// <summary>
/// Assembly-level opt-in for diagnostics about how filter leaf values are deserialized.
/// Settings-bag shape so future flags can be added without breaking changes.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed class FilterValueDiagnosticsAttribute : Attribute
{
    /// <summary>
    /// When <c>true</c>, the source generator emits <c>FN1008</c> for every
    /// <c>[FilterOperator]</c> value type and every <c>[PropertyMap]</c> override
    /// value type that is not registered (via <c>[JsonSerializable(typeof(T))]</c>)
    /// in any <c>JsonSerializerContext</c> visible in the current compilation.
    /// Defaults to <c>false</c>: reflection-fallback is the documented happy path,
    /// AOT-conscious consumers opt in.
    /// </summary>
    public bool WarnUnregistered { get; init; } = false;
}
