using System.Linq.Expressions;
using System.Text.Json;

namespace Filtering.Net;

// An operator already spliced onto one property's accessor. BuildBody returns a bool expression
// over the accessor's entity parameter.
internal abstract class BoundOperator
{
    public abstract void Validate(FilterLeaf leaf, string path, List<FilterValidationError> errors, JsonSerializerOptions? serializerOptions);

    public abstract Expression BuildBody(FilterLeaf leaf, JsonSerializerOptions? serializerOptions);
}

internal sealed class OperatorBinding<TColumn>(
    LambdaExpression accessor,
    INullableColumnSupport? nullableSupport,
    PropertyInterception<TColumn> interception,
    string propertyPath)
{
    public LambdaExpression Accessor { get; } = accessor;

    // Non-null exactly when the accessor returns Nullable<TColumn>.
    public INullableColumnSupport? NullableSupport { get; } = nullableSupport;

    public PropertyInterception<TColumn> Interception { get; } = interception;

    public string PropertyPath { get; } = propertyPath;
}

internal sealed class PropertyInterception<TColumn>(
    Func<InterceptContext, TColumn, TColumn>? scalarInterceptor,
    Func<InterceptContext, TColumn[], TColumn[]>? arrayInterceptor,
    Func<InterceptContext, JsonElement, TColumn>? rawInterceptor)
{
    public Func<InterceptContext, TColumn, TColumn>? ScalarInterceptor { get; } = scalarInterceptor;

    public Func<InterceptContext, TColumn[], TColumn[]>? ArrayInterceptor { get; } = arrayInterceptor;

    public Func<InterceptContext, JsonElement, TColumn>? RawInterceptor { get; } = rawInterceptor;
}
