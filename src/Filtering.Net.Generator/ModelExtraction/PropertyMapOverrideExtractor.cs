using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Filtering.Net.Generator;

// Reads the .Operator(...) calls of a [PropertyMap] method for diagnostics and typed-value detection.
// The method itself runs at filter construction, so bodies are never parsed for emission.
internal static class PropertyMapOverrideExtractor
{
    private const string FilterRuleMetadataName = "FilterRule";
    private const string FilterRuleBuilderMetadataName = "FilterRuleBuilder";

    public static PropertyOverrideModel Extract(
        IMethodSymbol methodSymbol,
        string propertyName,
        Compilation? compilation)
    {
        var operators = new List<OverrideOperatorModel>();

        var returnExpression = TryFindReturnExpression(methodSymbol);
        if (returnExpression is not null)
        {
            var semanticModel = TryGetSemanticModel(compilation, returnExpression.SyntaxTree);
            foreach (var invocation in CollectChainInvocations(returnExpression))
            {
                if (invocation.MethodName != "Operator") continue;
                var operatorModel = TryExtractOperator(invocation, semanticModel);
                if (operatorModel is not null) operators.Add(operatorModel);
            }
        }

        var hasTypedValueOperator = operators.Exists(overrideOperator => overrideOperator.ValueClrType is not null);

        return new PropertyOverrideModel(
            PropertyName: propertyName,
            MethodName: methodSymbol.Name,
            BuilderTypeFqn: TryGetBuilderTypeFqn(methodSymbol),
            Operators: new EquatableList<OverrideOperatorModel>(operators),
            HasTypedValueOperator: hasTypedValueOperator);
    }

    private static string? TryGetBuilderTypeFqn(IMethodSymbol methodSymbol)
    {
        if (!methodSymbol.IsStatic || methodSymbol.Parameters.Length != 1) return null;
        if (methodSymbol.ReturnType is not INamedTypeSymbol { Name: FilterRuleMetadataName, TypeArguments.Length: 2 }) return null;
        if (methodSymbol.Parameters[0].Type is not INamedTypeSymbol { Name: FilterRuleBuilderMetadataName, TypeArguments.Length: 2 } builderType) return null;
        return TypeNameFormatter.Format(builderType);
    }

    private static SemanticModel? TryGetSemanticModel(Compilation? compilation, SyntaxTree syntaxTree)
    {
        if (compilation is null) return null;
        try { return compilation.GetSemanticModel(syntaxTree); }
        catch (ArgumentException) { return null; }
    }

    private static ExpressionSyntax? TryFindReturnExpression(IMethodSymbol methodSymbol)
    {
        foreach (var syntaxReference in methodSymbol.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax() is not MethodDeclarationSyntax methodDeclaration) continue;
            if (methodDeclaration.ExpressionBody?.Expression is ExpressionSyntax arrowExpression)
            {
                return arrowExpression;
            }
            if (methodDeclaration.Body is not null)
            {
                foreach (var statement in methodDeclaration.Body.Statements)
                {
                    if (statement is ReturnStatementSyntax returnStatement
                        && returnStatement.Expression is not null)
                    {
                        return returnStatement.Expression;
                    }
                }
            }
        }
        return null;
    }

    private static IReadOnlyList<ChainInvocation> CollectChainInvocations(ExpressionSyntax expression)
    {
        var stack = new Stack<ChainInvocation>();
        var current = expression;
        while (current is InvocationExpressionSyntax invocation
            && invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            stack.Push(new ChainInvocation(memberAccess.Name.Identifier.Text, invocation));
            current = memberAccess.Expression;
        }
        return [.. stack];
    }

    private static OverrideOperatorModel? TryExtractOperator(ChainInvocation chainInvocation, SemanticModel? semanticModel)
    {
        var arguments = chainInvocation.Invocation.ArgumentList.Arguments;
        if (arguments.Count != 2) return null;
        if (arguments[0].Expression is not LiteralExpressionSyntax nameLiteral) return null;

        return new OverrideOperatorModel(
            Name: nameLiteral.Token.ValueText,
            ValueClrType: ResolveValueClrType(chainInvocation.Invocation, arguments[1].Expression, semanticModel),
            Location: LocationInfo.FromLocation(chainInvocation.Invocation.GetLocation()));
    }

    // Operator<TArgument> carries the value type as its type argument; the unary overload has none.
    // Without a semantic model the lambda's declared parameter type is the best available answer.
    private static string? ResolveValueClrType(InvocationExpressionSyntax invocation, ExpressionSyntax predicate, SemanticModel? semanticModel)
    {
        if (semanticModel?.GetSymbolInfo(invocation).Symbol is IMethodSymbol operatorMethod)
        {
            return operatorMethod.TypeArguments.Length == 1 ? TypeNameFormatter.Format(operatorMethod.TypeArguments[0]) : null;
        }
        if (predicate is ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters.Count: 2 } lambda)
        {
            return lambda.ParameterList.Parameters[1].Type?.ToString() ?? "object";
        }
        return null;
    }

    private sealed record ChainInvocation(string MethodName, InvocationExpressionSyntax Invocation);
}
