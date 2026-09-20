using System.Linq.Expressions;
using System.Reflection;

namespace Filtering.Net;

// Splices a Nullable<TColumn> accessor into an operator predicate written over TColumn, producing
// the lifted comparisons the C# compiler emits for "entity.NullableColumn == value". Anything the
// lifter does not recognise falls back to a conversion to TColumn, which providers translate as the
// column itself.
internal sealed class NullableColumnLifter : ExpressionVisitor
{
    private static readonly MethodInfo EnumerableContainsDefinition =
        ((MethodCallExpression)((Expression<Func<int[], int, bool>>)((values, column) => values.Contains(column))).Body)
        .Method.GetGenericMethodDefinition();

    private static readonly Dictionary<Type, Type> WellKnownNullableTypes = new()
    {
        [typeof(bool)] = typeof(bool?),
        [typeof(byte)] = typeof(byte?),
        [typeof(sbyte)] = typeof(sbyte?),
        [typeof(short)] = typeof(short?),
        [typeof(ushort)] = typeof(ushort?),
        [typeof(int)] = typeof(int?),
        [typeof(uint)] = typeof(uint?),
        [typeof(long)] = typeof(long?),
        [typeof(ulong)] = typeof(ulong?),
        [typeof(float)] = typeof(float?),
        [typeof(double)] = typeof(double?),
        [typeof(decimal)] = typeof(decimal?),
        [typeof(char)] = typeof(char?),
        [typeof(DateTime)] = typeof(DateTime?),
        [typeof(DateTimeOffset)] = typeof(DateTimeOffset?),
        [typeof(TimeSpan)] = typeof(TimeSpan?),
        [typeof(Guid)] = typeof(Guid?),
    };

    private readonly ParameterExpression _columnParameter;
    private readonly Expression _nullableAccessorBody;
    private readonly ParameterExpression? _valueParameter;
    private readonly INullableColumnSupport _nullableSupport;

    public NullableColumnLifter(
        ParameterExpression columnParameter,
        Expression nullableAccessorBody,
        ParameterExpression? valueParameter,
        INullableColumnSupport nullableSupport)
    {
        _columnParameter = columnParameter;
        _nullableAccessorBody = nullableAccessorBody;
        _valueParameter = valueParameter;
        _nullableSupport = nullableSupport;
    }

    // Set when a values.Contains(column) call was re-targeted; the caller must then supply a TColumn?[] value.
    public ParameterExpression? NullableArrayValueParameter { get; private set; }

    public Expression Lift(Expression predicateBody) => Visit(predicateBody)!;

    protected override Expression VisitParameter(ParameterExpression node) =>
        node == _columnParameter
            ? Expression.Convert(_nullableAccessorBody, _nullableSupport.ColumnType)
            : base.VisitParameter(node);

    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (!IsComparison(node.NodeType)) return base.VisitBinary(node);

        var liftedLeft = TryLiftColumnOperand(node.Left);
        var liftedRight = TryLiftColumnOperand(node.Right);
        if (liftedLeft is null && liftedRight is null) return base.VisitBinary(node);

        var leftOperand = liftedLeft ?? TryMakeNullable(Visit(node.Left)!);
        var rightOperand = liftedRight ?? TryMakeNullable(Visit(node.Right)!);
        if (leftOperand is null || rightOperand is null || leftOperand.Type != rightOperand.Type)
        {
            return base.VisitBinary(node);
        }
        return Expression.MakeBinary(node.NodeType, leftOperand, rightOperand, liftToNull: false, node.Method);
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (IsNullableValueType(node.Type) && TryLiftColumnOperand(node) is { } liftedConversion)
        {
            return liftedConversion;
        }
        return base.VisitUnary(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (_valueParameter is not null
            && node.Method.IsGenericMethod
            && node.Method.GetGenericMethodDefinition() == EnumerableContainsDefinition
            && StripConversion(node.Arguments[0]) == _valueParameter
            && node.Arguments[1] == _columnParameter
            && _valueParameter.Type.IsArray
            && _valueParameter.Type.GetElementType() == _nullableSupport.ColumnType)
        {
            NullableArrayValueParameter ??= Expression.Parameter(_nullableSupport.NullableArrayType, _valueParameter.Name);
            return Expression.Call(_nullableSupport.NullableContainsMethod, NullableArrayValueParameter, _nullableAccessorBody);
        }
        return base.VisitMethodCall(node);
    }

    private Expression? TryLiftColumnOperand(Expression operand)
    {
        if (operand == _columnParameter) return _nullableAccessorBody;

        if (operand is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } conversion
            && conversion.Operand == _columnParameter)
        {
            if (conversion.Type == _nullableAccessorBody.Type) return _nullableAccessorBody;
            var nullableTargetType = IsNullableValueType(conversion.Type) ? conversion.Type : TryGetNullableType(conversion.Type);
            return nullableTargetType is null ? null : Expression.Convert(_nullableAccessorBody, nullableTargetType);
        }
        return null;
    }

    private Expression? TryMakeNullable(Expression operand)
    {
        if (!operand.Type.IsValueType || IsNullableValueType(operand.Type)) return operand;
        var nullableType = TryGetNullableType(operand.Type);
        return nullableType is null ? null : Expression.Convert(operand, nullableType);
    }

    private Type? TryGetNullableType(Type valueType)
    {
        if (valueType == _nullableSupport.ColumnType) return _nullableSupport.NullableColumnType;
        return WellKnownNullableTypes.TryGetValue(valueType, out var nullableType) ? nullableType : null;
    }

    private static Expression StripConversion(Expression expression) =>
        expression is UnaryExpression { NodeType: ExpressionType.Convert } conversion ? conversion.Operand : expression;

    private static bool IsNullableValueType(Type type) => Nullable.GetUnderlyingType(type) is not null;

    private static bool IsComparison(ExpressionType nodeType) => nodeType is
        ExpressionType.Equal or ExpressionType.NotEqual
        or ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual
        or ExpressionType.LessThan or ExpressionType.LessThanOrEqual;
}
