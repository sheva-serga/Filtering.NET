namespace Filtering.Net.Generator;

internal static class ProfileExtractorEmitter
{
    public static string EmitScalarCall(
        PropertyValueShape valueShape,
        string elementExpression,
        string outValueIdentifier,
        string outErrorIdentifier) =>
        $"global::{valueShape.ProfileFullName}.TryGetValue({elementExpression}, out var {outValueIdentifier}, out var {outErrorIdentifier})";

    public static string EmitScalarCallDiscardingValue(
        PropertyValueShape valueShape,
        string elementExpression,
        string outErrorIdentifier) =>
        $"global::{valueShape.ProfileFullName}.TryGetValue({elementExpression}, out _, out var {outErrorIdentifier})";

    public static string EmitArrayCall(
        PropertyValueShape valueShape,
        string elementExpression,
        string outValuesIdentifier,
        string outErrorIdentifier) =>
        $"global::{valueShape.ProfileFullName}.TryGetArray({elementExpression}, out var {outValuesIdentifier}, out var {outErrorIdentifier})";

    public static string EmitArrayCallDiscardingValue(
        PropertyValueShape valueShape,
        string elementExpression,
        string outErrorIdentifier) =>
        $"global::{valueShape.ProfileFullName}.TryGetArray({elementExpression}, out _, out var {outErrorIdentifier})";
}
