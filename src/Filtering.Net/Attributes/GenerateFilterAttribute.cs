namespace Filtering.Net;

/// <summary>Marks a partial class as a filter definition for <typeparamref name="TEntity"/>. The source generator emits the implementation.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GenerateFilterAttribute<TEntity> : Attribute
{
    /// <summary>The entity type this filter targets.</summary>
    public Type EntityType => typeof(TEntity);
}
