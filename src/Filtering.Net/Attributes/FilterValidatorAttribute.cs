namespace Filtering.Net;

/// <summary>Marks a static method as a value validator for one operator on the enclosing profile. Returns null on success, an error message otherwise.</summary>
/// <param name="operatorName">Name of the operator validated by this method.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class FilterValidatorAttribute(string operatorName) : Attribute
{
    /// <summary>The operator name this validator runs for.</summary>
    public string OperatorName { get; } = operatorName;
}
