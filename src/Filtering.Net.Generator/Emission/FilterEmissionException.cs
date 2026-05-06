namespace Filtering.Net.Generator;

// Thrown when an internal generator invariant is violated (a bug, not user input).
// Custom type keeps it grep-able vs generic InvalidOperationException.
internal sealed class FilterEmissionException(string message) : Exception(message)
{
}
