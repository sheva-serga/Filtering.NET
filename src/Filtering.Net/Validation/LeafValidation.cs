namespace Filtering.Net;

/// <summary>
/// Helpers used by the source generator's emitted leaf validators to add a
/// <see cref="FilterValidationError"/> to the running list. Centralises the error-shape
/// boilerplate (Path / Code / Message / Field / OperatorName) so the emitter's
/// shape-grouped switch can stay short — see Refactor 3 in the v1 cleanup pass.
/// </summary>
public static class LeafValidation
{
    /// <summary>Records a value-shape mismatch error on the given <paramref name="leaf"/>.
    /// Used when a <c>TryGet*</c> helper rejects the JSON value as the wrong type.</summary>
    /// <param name="errors">The running error list to append to.</param>
    /// <param name="leaf">The leaf whose value failed extraction.</param>
    /// <param name="path">The JSON-pointer-like location of the leaf.</param>
    /// <param name="typeError">The error message returned by the <c>TryGet*</c> helper.</param>
    public static void AddTypeError(List<FilterValidationError> errors, FilterLeaf leaf, string path, string typeError) =>
        errors.Add(new FilterValidationError(
            $"{path}.value",
            FilterValidationCode.InvalidValueType,
            typeError,
            Field: leaf.Field,
            OperatorName: leaf.Operator));

    /// <summary>Records an "operator not allowed on this field" error.
    /// The default arm of a per-property leaf-validator switch.</summary>
    /// <param name="errors">The running error list to append to.</param>
    /// <param name="leaf">The leaf whose operator is unsupported.</param>
    /// <param name="path">The JSON-pointer-like location of the leaf.</param>
    /// <param name="fieldName">The configured property name being validated against.</param>
    public static void AddOperatorError(List<FilterValidationError> errors, FilterLeaf leaf, string path, string fieldName) =>
        errors.Add(new FilterValidationError(
            $"{path}.op",
            FilterValidationCode.OperatorNotAllowed,
            $"Operator '{leaf.Operator}' is not supported on field '{fieldName}'.",
            Field: leaf.Field,
            OperatorName: leaf.Operator));

    /// <summary>Records an "operator takes no value" error for unary operators
    /// (<c>isNull</c> and any custom unary operators) when the caller passed a non-null value.</summary>
    /// <param name="errors">The running error list to append to.</param>
    /// <param name="leaf">The leaf whose operator was given a value despite being unary.</param>
    /// <param name="path">The JSON-pointer-like location of the leaf.</param>
    public static void AddNoValueError(List<FilterValidationError> errors, FilterLeaf leaf, string path) =>
        errors.Add(new FilterValidationError(
            $"{path}.value",
            FilterValidationCode.InvalidValueType,
            $"Operator '{leaf.Operator}' takes no value.",
            Field: leaf.Field,
            OperatorName: leaf.Operator));

    /// <summary>Records an interceptor-rejection error. Used when an
    /// <c>[InterceptValue]</c> dry-run threw a <see cref="FilterValidationException"/>.</summary>
    /// <param name="errors">The running error list to append to.</param>
    /// <param name="leaf">The leaf whose value the interceptor rejected.</param>
    /// <param name="path">The JSON-pointer-like location of the leaf.</param>
    /// <param name="message">The interceptor's rejection message.</param>
    public static void AddInterceptorError(List<FilterValidationError> errors, FilterLeaf leaf, string path, string message) =>
        errors.Add(new FilterValidationError(
            $"{path}.value",
            FilterValidationCode.InterceptorRejected,
            message,
            Field: leaf.Field,
            OperatorName: leaf.Operator));

    /// <summary>Method-group target for a profile's <c>TryGetValue</c> extractor — the
    /// scalar half of the <see cref="ValidateMappedLeaf{TValue}"/> dispatcher.</summary>
    public delegate bool TryParseScalar<TValue>(System.Text.Json.JsonElement element, out TValue value, out string error);

    /// <summary>Method-group target for a profile's <c>TryGetArray</c> extractor — the
    /// array half of the <see cref="ValidateMappedLeaf{TValue}"/> dispatcher.</summary>
    public delegate bool TryParseArray<TValue>(System.Text.Json.JsonElement element, out TValue[] values, out string error);

    /// <summary>Validates a leaf for a vanilla mapped property (no interceptor, no custom
    /// operators, no <c>[PropertyMap]</c> override). The generator emits a one-line
    /// forwarder per such property; this helper performs the operator-shape dispatch and
    /// translates extractor failures into <see cref="FilterValidationError"/>s. Properties
    /// with bespoke validation logic (interceptors, custom operators, overrides) keep their
    /// inlined switch.</summary>
    /// <param name="leaf">The leaf being validated.</param>
    /// <param name="path">JSON-pointer-like location of the leaf.</param>
    /// <param name="errors">Running error list to append to.</param>
    /// <param name="propertyName">Configured property name — surfaced in the
    /// "operator not allowed" default arm.</param>
    /// <param name="allowedScalarOps">Uppercase names of allowed scalar-shape operators.</param>
    /// <param name="allowedArrayOps">Uppercase names of allowed array-shape operators.</param>
    /// <param name="allowedNoneOps">Uppercase names of allowed unary (no-value) operators.</param>
    /// <param name="scalarExtractor">Profile's <c>TryGetValue</c> method group — may be
    /// null when no scalar operators are configured.</param>
    /// <param name="arrayExtractor">Profile's <c>TryGetArray</c> method group — may be
    /// null when no array operators are configured.</param>
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
