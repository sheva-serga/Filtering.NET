using System.Linq.Expressions;

namespace Filtering.Net;

// Splices a Nullable<TColumn> accessor into an operator predicate written over TColumn, producing
// the lifted comparisons the C# compiler emits for "entity.NullableColumn == value". Anything the
// lifter does not recognise falls back to a conversion to TColumn, which providers translate as the
// column itself.
internal sealed class NullableColumnLifter : ExpressionVisitor
{
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
        if (IsValueArrayContainsColumn(node))
        {
            NullableArrayValueParameter ??= Expression.Parameter(_nullableSupport.NullableArrayType, _valueParameter!.Name);
            return Expression.Call(_nullableSupport.NullableContainsMethod, NullableArrayValueParameter, _nullableAccessorBody);
        }
        return base.VisitMethodCall(node);
    }

    // Matches "values.Contains(column)" however the consumer's compiler bound it: Enumerable.Contains over
    // the array, or MemoryExtensions.Contains over an implicit span conversion (first-class spans).
    private bool IsValueArrayContainsColumn(MethodCallExpression node)
    {
        if (_valueParameter is null || !_valueParameter.Type.IsArray) return false;
        if (_valueParameter.Type.GetElementType() != _nullableSupport.ColumnType) return false;
        if (!node.Method.IsStatic || node.Method.Name != nameof(Enumerable.Contains)) return false;
        if (node.Method.DeclaringType != typeof(Enumerable) && node.Method.DeclaringType?.FullName != "System.MemoryExtensions") return false;
        if (node.Arguments.Count < 2 || node.Arguments[1] != _columnParameter) return false;
        if (StripConversion(node.Arguments[0]) != _valueParameter) return false;

        // A trailing comparer argument is only safe to drop when it is the default (null) comparer.
        for (var argumentIndex = 2; argumentIndex < node.Arguments.Count; argumentIndex++)
        {
            if (node.Arguments[argumentIndex] is not (ConstantExpression { Value: null } or DefaultExpression)) return false;
        }
        return true;
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

    // Array-to-span conversions appear either as a Convert node or as a call to the span's op_Implicit.
    private static Expression StripConversion(Expression expression) => expression switch
    {
        UnaryExpression { NodeType: ExpressionType.Convert } conversion => conversion.Operand,
        MethodCallExpression { Method.Name: "op_Implicit", Arguments.Count: 1 } implicitConversion => implicitConversion.Arguments[0],
        _ => expression,
    };

    private static bool IsNullableValueType(Type type) => Nullable.GetUnderlyingType(type) is not null;

    private static bool IsComparison(ExpressionType nodeType) => nodeType is
        ExpressionType.Equal or ExpressionType.NotEqual
        or ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual
        or ExpressionType.LessThan or ExpressionType.LessThanOrEqual;
}
