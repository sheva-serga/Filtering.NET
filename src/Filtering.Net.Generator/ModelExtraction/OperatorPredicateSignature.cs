using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator;

// The shape of a [FilterOperator] member, read from its declared type:
// Expression<Func<TColumn, bool>> (unary) or Expression<Func<TColumn, TValue, bool>> (value).
internal sealed record OperatorPredicateSignature(ITypeSymbol ColumnType, ITypeSymbol? ValueType)
{
    public static OperatorPredicateSignature? TryRead(ISymbol operatorMember)
    {
        ITypeSymbol? memberType = operatorMember switch
        {
            IPropertySymbol property => property.Type,
            IMethodSymbol { Parameters.Length: 0 } method => method.ReturnType,
            _ => null,
        };
        if (memberType is not INamedTypeSymbol { Name: "Expression", TypeArguments.Length: 1 } expressionType) return null;
        if (expressionType.TypeArguments[0] is not INamedTypeSymbol { Name: "Func" } funcType) return null;

        return funcType.TypeArguments.Length switch
        {
            2 => new OperatorPredicateSignature(funcType.TypeArguments[0], ValueType: null),
            3 => new OperatorPredicateSignature(funcType.TypeArguments[0], funcType.TypeArguments[1]),
            _ => null,
        };
    }
}
