using System.Linq.Expressions;
using System.Reflection;

namespace Filtering.Net;

// Type-erased view of a Nullable<TColumn> column, so operator code that has no struct constraint
// on TColumn can still build lifted expressions without MakeGenericType / MakeGenericMethod.
internal interface INullableColumnSupport
{
    Type ColumnType { get; }

    Type NullableColumnType { get; }

    Type NullableArrayType { get; }

    MethodInfo NullableContainsMethod { get; }

    Expression CreateNullableArrayExpression(object values);
}

internal sealed class NullableColumnSupport<TColumn> : INullableColumnSupport
    where TColumn : struct
{
    public static readonly NullableColumnSupport<TColumn> Instance = new();

    private static readonly MethodInfo ContainsMethod =
        ((MethodCallExpression)((Expression<Func<TColumn?[], TColumn?, bool>>)((values, column) => values.Contains(column))).Body).Method;

    public Type ColumnType => typeof(TColumn);

    public Type NullableColumnType => typeof(TColumn?);

    public Type NullableArrayType => typeof(TColumn?[]);

    public MethodInfo NullableContainsMethod => ContainsMethod;

    public Expression CreateNullableArrayExpression(object values)
    {
        var sourceValues = (TColumn[])values;
        var nullableValues = new TColumn?[sourceValues.Length];
        for (var valueIndex = 0; valueIndex < sourceValues.Length; valueIndex++)
        {
            nullableValues[valueIndex] = sourceValues[valueIndex];
        }
        return FilterValueHolder<TColumn?[]>.AsExpression(nullableValues);
    }
}
