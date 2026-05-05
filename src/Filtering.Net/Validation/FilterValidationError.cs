namespace Filtering.Net;

/// <summary>A single error from filter request validation. <see cref="Path"/> is JSON-pointer-like (e.g., "where.and[0].value").</summary>
/// <param name="Path">JSON-pointer-like location of the offending node within the request.</param>
/// <param name="Code">Machine-readable category for the error.</param>
/// <param name="Message">Human-readable explanation suitable for surfacing to API clients.</param>
/// <param name="Field">The configured field name involved, if applicable.</param>
/// <param name="OperatorName">The operator involved, if applicable.</param>
public sealed record FilterValidationError(
    string Path,
    FilterValidationCode Code,
    string Message,
    string? Field = null,
    string? OperatorName = null);
