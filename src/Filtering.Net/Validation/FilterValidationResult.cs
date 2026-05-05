namespace Filtering.Net;

/// <summary>Aggregate result of validating a <see cref="FilterRequest"/>. Use <see cref="IsValid"/> to check; iterate <see cref="Errors"/> for details.</summary>
/// <param name="Errors">All validation errors detected for the request.</param>
public sealed record FilterValidationResult(IReadOnlyList<FilterValidationError> Errors)
{
    /// <summary>True when no errors were collected.</summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>A shared empty success result.</summary>
    public static FilterValidationResult Success { get; } = new([]);
}
