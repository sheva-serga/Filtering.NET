namespace Filtering.Net;

/// <summary>Marks a static lambda property (or method) on a profile as an operator template. The source generator extracts the body and inlines it.</summary>
/// <remarks>Initializes a new <see cref="FilterOperatorAttribute"/>.</remarks>
/// <param name="name">Operator name (e.g., "eq", "contains", "withinDays").</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class FilterOperatorAttribute(string name) : Attribute
{
    /// <summary>The operator name as it appears in filter requests.</summary>
    public string Name { get; } = name;
}
