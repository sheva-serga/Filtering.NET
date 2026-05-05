namespace Filtering.Net.Generator;

/// <summary>Thrown by emission-layer helpers when an internal invariant is violated
/// (e.g., an unrecognised <see cref="PropertyValueShape"/> reaches a per-shape switch).
/// These cases indicate a bug in the generator itself rather than user input — a custom
/// type makes them grep-able and avoids drowning in generic <c>InvalidOperationException</c>.
/// </summary>
/// <remarks>Creates a new <see cref="FilterEmissionException"/> with the given message.</remarks>
internal sealed class FilterEmissionException(string message) : Exception(message)
{
}
