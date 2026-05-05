namespace Filtering.Net.Generator;

/// <summary>Model describing a value interceptor method discovered on a filter class. <see cref="ValueClrType"/> is null when the user-declared method has no parameters (a malformed interceptor that the generator skips with a diagnostic).</summary>
internal sealed record InterceptorModel(
    string PropertyName,
    string MethodName,
    bool Raw,
    string? ValueClrType);
