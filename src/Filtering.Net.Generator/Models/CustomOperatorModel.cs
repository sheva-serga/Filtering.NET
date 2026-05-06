namespace Filtering.Net.Generator;

// Pre-extracted by ProfileResolver so the emitter can inline lambda bodies without re-walking syntax.
// Built-in profile operators go through BuiltInProfileCatalog instead; no instances here.
internal sealed record CustomOperatorModel(
    string OperatorName,
    string DeclaringProfileFullName,
    string ColumnParameterName,
    string? ValueParameterName,
    string? ValueClrType,
    bool IsArrayValue,
    string LambdaBodyCSharp,
    LocationInfo? Location);
