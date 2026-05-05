namespace Filtering.Net;

/// <summary>Context passed to value interceptors. Identifies which property/alias/operator the value belongs to.</summary>
/// <param name="PropertyPath">Dotted path of the underlying entity property (e.g., "User.Address.City").</param>
/// <param name="Alias">The configured public alias for the field.</param>
/// <param name="Operator">The operator being applied (e.g., "eq", "contains").</param>
public readonly record struct InterceptContext(
    string PropertyPath,
    string Alias,
    string Operator);
