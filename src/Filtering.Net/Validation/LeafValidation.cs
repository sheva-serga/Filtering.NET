namespace Filtering.Net;

/// <summary>Helpers used by emitted leaf validators to append structured <see cref="FilterValidationError"/> entries.</summary>
public static class LeafValidation
{
    /// <summary>Appends a value-type mismatch error when a <c>TryGet*</c> extractor rejects the JSON value.</summary>
    public static void AddTypeError(List<FilterValidationError> errors, FilterLeaf leaf, string path, string typeError) =>
        errors.Add(new FilterValidationError(
            $"{path}.value",
            FilterValidationCode.InvalidValueType,
            typeError,
            Field: leaf.Field,
            OperatorName: leaf.Operator));

    /// <summary>Appends an "operator not allowed" error; used as the default arm of a per-property switch.</summary>
    public static void AddOperatorError(List<FilterValidationError> errors, FilterLeaf leaf, string path, string fieldName) =>
        errors.Add(new FilterValidationError(
            $"{path}.op",
            FilterValidationCode.OperatorNotAllowed,
            $"Operator '{leaf.Operator}' is not supported on field '{fieldName}'.",
            Field: leaf.Field,
            OperatorName: leaf.Operator));

    /// <summary>Appends an error when a unary operator (e.g. <c>isNull</c>) receives a non-null value.</summary>
    public static void AddNoValueError(List<FilterValidationError> errors, FilterLeaf leaf, string path) =>
        errors.Add(new FilterValidationError(
            $"{path}.value",
            FilterValidationCode.InvalidValueType,
            $"Operator '{leaf.Operator}' takes no value.",
            Field: leaf.Field,
            OperatorName: leaf.Operator));

    /// <summary>Appends an interceptor-rejection error when an <c>[InterceptValue]</c> method throws.</summary>
    public static void AddInterceptorError(List<FilterValidationError> errors, FilterLeaf leaf, string path, string message) =>
        errors.Add(new FilterValidationError(
            $"{path}.value",
            FilterValidationCode.InterceptorRejected,
            message,
            Field: leaf.Field,
            OperatorName: leaf.Operator));

    /// <summary>Method-group delegate for a profile's scalar <c>TryGetValue</c> extractor.</summary>
    public delegate bool TryParseScalar<TValue>(System.Text.Json.JsonElement element, out TValue value, out string error);

    /// <summary>Method-group delegate for a profile's array <c>TryGetArray</c> extractor.</summary>
    public delegate bool TryParseArray<TValue>(System.Text.Json.JsonElement element, out TValue[] values, out string error);

    /// <summary>Validates a plain mapped leaf (no interceptor, no custom operators, no <c>[PropertyMap]</c> override); emitted code forwards here to avoid inlining the operator-shape switch per property.</summary>
    public static void ValidateMappedLeaf<TValue>(
        FilterLeaf leaf,
        string path,
        List<FilterValidationError> errors,
        string propertyName,
        string[] allowedScalarOps,
        string[] allowedArrayOps,
        string[] allowedNoneOps,
        TryParseScalar<TValue>? scalarExtractor,
        TryParseArray<TValue>? arrayExtractor)
    {
        var operatorKey = leaf.Operator.ToUpperInvariant();
        if (scalarExtractor is not null && Array.IndexOf(allowedScalarOps, operatorKey) >= 0)
        {
            if (!scalarExtractor(leaf.Value, out _, out var typeError))
                AddTypeError(errors, leaf, path, typeError);
            return;
        }
        if (arrayExtractor is not null && Array.IndexOf(allowedArrayOps, operatorKey) >= 0)
        {
            if (!arrayExtractor(leaf.Value, out _, out var arrayError))
                AddTypeError(errors, leaf, path, arrayError);
            return;
        }
        if (Array.IndexOf(allowedNoneOps, operatorKey) >= 0)
        {
            if (leaf.Value.ValueKind is not System.Text.Json.JsonValueKind.Null and not System.Text.Json.JsonValueKind.Undefined)
                AddNoValueError(errors, leaf, path);
            return;
        }
        AddOperatorError(errors, leaf, path, propertyName);
    }
}
