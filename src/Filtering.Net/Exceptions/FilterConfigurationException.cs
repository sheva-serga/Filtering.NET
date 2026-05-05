namespace Filtering.Net;

/// <summary>Thrown when a filter or profile is misconfigured at runtime. Most cases are caught by the analyzer at compile time.</summary>
public sealed class FilterConfigurationException : FilteringException
{
    /// <summary>Initializes a new <see cref="FilterConfigurationException"/> with the specified message.</summary>
    public FilterConfigurationException(string message) : base(message) { }

    /// <summary>Initializes a new <see cref="FilterConfigurationException"/> with the specified message and inner exception.</summary>
    public FilterConfigurationException(string message, Exception innerException) : base(message, innerException) { }
}
