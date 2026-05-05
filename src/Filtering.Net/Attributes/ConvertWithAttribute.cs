namespace Filtering.Net;

/// <summary>
/// Indicates that the property uses an EF Core <c>ValueConverter&lt;TModel, TProvider&gt;</c>.
/// The source generator uses the converter's TModel as the typed value parameter for the property.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class ConvertWithAttribute<TConverter> : Attribute
{
    /// <summary>The EF Core <c>ValueConverter</c> type configured for the property.</summary>
    public Type ConverterType => typeof(TConverter);
}
