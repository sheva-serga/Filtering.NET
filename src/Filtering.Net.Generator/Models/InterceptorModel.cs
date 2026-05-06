namespace Filtering.Net.Generator;

// ValueClrType is null when the method has fewer than two parameters (malformed; generator skips it).
internal sealed record InterceptorModel(
    string PropertyName,
    string MethodName,
    bool Raw,
    string? ValueClrType);
