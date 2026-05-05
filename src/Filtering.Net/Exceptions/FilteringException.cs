namespace Filtering.Net;

/// <summary>Base class for all Filtering.Net exceptions.</summary>
public abstract class FilteringException : Exception
{
    /// <summary>Initializes a new <see cref="FilteringException"/> with the specified message.</summary>
    protected FilteringException(string message) : base(message) { }

    /// <summary>Initializes a new <see cref="FilteringException"/> with the specified message and inner exception.</summary>
    protected FilteringException(string message, Exception innerException) : base(message, innerException) { }
}
