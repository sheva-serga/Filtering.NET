namespace Filtering.Net.Generator;

/// <summary>
/// Builds the C# expression that calls a runtime profile's <c>TryGetValue</c>/<c>TryGetArray</c>
/// helper for a given <see cref="PropertyValueShape"/>. Convention-based: the emitted call is
/// always <c>{ProfileFullName}.TryGetValue(...)</c> (or <c>TryGetArray</c>), regardless of
/// whether the resolved profile is a built-in (<c>StringFilter</c>, <c>Int32Filter</c>, …),
/// a user-defined custom profile, or an auto-emitted per-enum profile.
/// </summary>
internal static class ProfileExtractorEmitter
{
    /// <summary>Returns a fragment like
    /// <c>global::Filtering.Net.Int32Filter.TryGetValue(leaf.Value, out var typedValue, out var typeError)</c>
    /// for the resolved profile in <paramref name="valueShape"/>.</summary>
    public static string EmitScalarCall(
        PropertyValueShape valueShape,
        string elementExpression,
        string outValueIdentifier,
        string outErrorIdentifier) =>
        $"global::{valueShape.ProfileFullName}.TryGetValue({elementExpression}, out var {outValueIdentifier}, out var {outErrorIdentifier})";

    /// <summary>Variant of <see cref="EmitScalarCall"/> that emits a discard for the value
    /// out parameter — used by the validation pass which only cares about the success
    /// flag and any error message.</summary>
    public static string EmitScalarCallDiscardingValue(
        PropertyValueShape valueShape,
        string elementExpression,
        string outErrorIdentifier) =>
        $"global::{valueShape.ProfileFullName}.TryGetValue({elementExpression}, out _, out var {outErrorIdentifier})";

    /// <summary>Returns a fragment like
    /// <c>global::Filtering.Net.Int32Filter.TryGetArray(leaf.Value, out var typedValues, out var arrayError)</c>
    /// for the resolved profile in <paramref name="valueShape"/>.</summary>
    public static string EmitArrayCall(
        PropertyValueShape valueShape,
        string elementExpression,
        string outValuesIdentifier,
        string outErrorIdentifier) =>
        $"global::{valueShape.ProfileFullName}.TryGetArray({elementExpression}, out var {outValuesIdentifier}, out var {outErrorIdentifier})";

    /// <summary>Discard variant of <see cref="EmitArrayCall"/> for the validation pass.</summary>
    public static string EmitArrayCallDiscardingValue(
        PropertyValueShape valueShape,
        string elementExpression,
        string outErrorIdentifier) =>
        $"global::{valueShape.ProfileFullName}.TryGetArray({elementExpression}, out _, out var {outErrorIdentifier})";
}
