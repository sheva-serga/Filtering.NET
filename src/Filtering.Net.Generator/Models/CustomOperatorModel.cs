namespace Filtering.Net.Generator;

// An operator declared on a user profile. ValueClrType is null for unary operators; a non-null
// value type means the operator takes a typed value deserialized through the JSON resolver.
internal sealed record CustomOperatorModel(
    string OperatorName,
    string DeclaringProfileFullName,
    string? ValueClrType,
    LocationInfo? Location);
