namespace Filtering.Net;

/// <summary>Assembly-level opt-in for deserialization diagnostics; add flags here without breaking changes.</summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed class FilterValueDiagnosticsAttribute : Attribute
{
    /// <summary>When <c>true</c>, emits <c>FN1008</c> for value types not registered in a visible <c>JsonSerializerContext</c>. Default <c>false</c>; AOT-conscious consumers opt in.</summary>
    public bool WarnUnregistered { get; init; } = false;
}
