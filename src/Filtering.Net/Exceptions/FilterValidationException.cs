namespace Filtering.Net;

/// <summary>Thrown when a <see cref="FilterRequest"/> fails validation. Carries structured error details.</summary>
public sealed class FilterValidationException : FilteringException
{
    /// <summary>The structured validation result containing all errors.</summary>
    public FilterValidationResult Result { get; }

    /// <summary>Initializes a new <see cref="FilterValidationException"/> wrapping a validation result.</summary>
    /// <param name="result">A non-null, invalid <see cref="FilterValidationResult"/>. Constructing this exception from a valid result is a programming error.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="result"/> is null.</exception>
    /// <exception cref="ArgumentException">When <paramref name="result"/> reports IsValid (no errors).</exception>
    public FilterValidationException(FilterValidationResult result)
        : base(BuildMessage(result ?? throw new ArgumentNullException(nameof(result))))
    {
        if (result.IsValid)
            throw new ArgumentException(
                "Cannot construct FilterValidationException from a valid result (no errors).",
                nameof(result));
        Result = result;
    }

    private static string BuildMessage(FilterValidationResult result)
    {
        if (result.Errors.Count == 0)
            return "Filter request validation produced an empty error list.";
        var errorWord = result.Errors.Count == 1 ? "error" : "errors";
        var firstError = result.Errors[0];
        return $"Filter request is invalid ({result.Errors.Count} {errorWord}). First: {firstError.Path} — {firstError.Message}";
    }
}
