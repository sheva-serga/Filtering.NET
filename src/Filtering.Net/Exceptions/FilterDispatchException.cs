namespace Filtering.Net;

/// <summary>Thrown from generated dispatch fall-throughs after validation. Indicates an internal bug in the generator.</summary>
public sealed class FilterDispatchException : FilteringException
{
    /// <summary>Initializes a new <see cref="FilterDispatchException"/> with the specified message.</summary>
    public FilterDispatchException(string message) : base(message) { }

    /// <summary>Initializes a new <see cref="FilterDispatchException"/> with the specified message and inner exception.</summary>
    public FilterDispatchException(string message, Exception innerException) : base(message, innerException) { }
}
