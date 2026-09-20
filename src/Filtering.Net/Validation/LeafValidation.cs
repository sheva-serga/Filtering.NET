namespace Filtering.Net;

// Shapes the per-leaf validation errors so every operator reports them identically.
internal static class LeafValidation
{
    public static void AddTypeError(List<FilterValidationError> errors, FilterLeaf leaf, string path, string typeError) =>
        errors.Add(new FilterValidationError(
            $"{path}.value",
            FilterValidationCode.InvalidValueType,
            typeError,
            Field: leaf.Field,
            OperatorName: leaf.Operator));

    public static void AddOperatorError(List<FilterValidationError> errors, FilterLeaf leaf, string path, string fieldName) =>
        errors.Add(new FilterValidationError(
            $"{path}.op",
            FilterValidationCode.OperatorNotAllowed,
            $"Operator '{leaf.Operator}' is not supported on field '{fieldName}'.",
            Field: leaf.Field,
            OperatorName: leaf.Operator));

    public static void AddNoValueError(List<FilterValidationError> errors, FilterLeaf leaf, string path) =>
        errors.Add(new FilterValidationError(
            $"{path}.value",
            FilterValidationCode.InvalidValueType,
            $"Operator '{leaf.Operator}' takes no value.",
            Field: leaf.Field,
            OperatorName: leaf.Operator));

    public static void AddInterceptorError(List<FilterValidationError> errors, FilterLeaf leaf, string path, string message) =>
        errors.Add(new FilterValidationError(
            $"{path}.value",
            FilterValidationCode.InterceptorRejected,
            message,
            Field: leaf.Field,
            OperatorName: leaf.Operator));
}
