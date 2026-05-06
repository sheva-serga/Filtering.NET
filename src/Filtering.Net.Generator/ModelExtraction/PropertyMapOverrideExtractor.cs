using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Filtering.Net.Generator;

// Extracts the builder.For(...).Operator(...) chain from a [PropertyMap] method body.
// Best-effort: non-conforming body shapes return an empty model; the emitter falls back to a stub.
internal static class PropertyMapOverrideExtractor
{
    public static PropertyOverrideModel Extract(
        IMethodSymbol methodSymbol,
        string propertyName,
        Compilation? compilation)
    {
        var operators = new List<OverrideOperatorModel>();
        string accessorBody = string.Empty;
        string entityParameterName = string.Empty;

        var returnExpression = TryFindReturnExpression(methodSymbol);
        if (returnExpression is not null)
        {
            var invocations = CollectChainInvocations(returnExpression);
            for (var invocationIndex = 0; invocationIndex < invocations.Count; invocationIndex++)
            {
                var invocation = invocations[invocationIndex];
                var methodName = invocation.MethodName;
                if (methodName == "For")
                {
                    if (invocation.Arguments.Count == 1
                        && invocation.Arguments[0] is SimpleLambdaExpressionSyntax forLambda)
                    {
                        entityParameterName = forLambda.Parameter.Identifier.Text;
                        accessorBody = ProfileLambdaQualifier.QualifyLambdaBody(forLambda.Body, compilation);
                    }
                }
                else if (methodName == "Operator")
                {
                    var operatorModel = TryExtractOperator(invocation, compilation);
                    if (operatorModel is not null) operators.Add(operatorModel);
                }
            }
        }

        var hasTypedValueOperator = operators.Exists(overrideOperator => overrideOperator.ValueClrType is not null);

        return new PropertyOverrideModel(
            PropertyName: propertyName,
            MethodName: methodSymbol.Name,
            PropertyAccessorBodyCSharp: accessorBody,
            EntityParameterName: entityParameterName,
            Operators: new EquatableList<OverrideOperatorModel>(operators),
            HasTypedValueOperator: hasTypedValueOperator);
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
            var methodName = memberAccess.Name.Identifier.Text;
            var arguments = invocation.ArgumentList.Arguments
                .Select(argument => argument.Expression)
                .ToList();
            stack.Push(new ChainInvocation(methodName, arguments, invocation.GetLocation()));
            current = memberAccess.Expression;
        }
        return [.. stack];
    }

    private static OverrideOperatorModel? TryExtractOperator(ChainInvocation invocation, Compilation? compilation)
    {
        if (invocation.Arguments.Count != 2) return null;

        if (invocation.Arguments[0] is not LiteralExpressionSyntax nameLiteral) return null;
        var operatorName = nameLiteral.Token.ValueText;

        var lambda = invocation.Arguments[1];
        var declarationLocation = LocationInfo.FromLocation(invocation.InvocationLocation);
        switch (lambda)
        {
            case ParenthesizedLambdaExpressionSyntax parenLambda:
                return BuildOperatorFromParenLambda(operatorName, parenLambda, compilation, declarationLocation);
            case SimpleLambdaExpressionSyntax simpleLambda:
                return BuildOperatorFromSimpleLambda(operatorName, simpleLambda, compilation, declarationLocation);
            default:
                return null;
        }
    }

    private static OverrideOperatorModel? BuildOperatorFromParenLambda(
        string operatorName,
        ParenthesizedLambdaExpressionSyntax lambda,
        Compilation? compilation,
        LocationInfo? declarationLocation)
    {
        var parameters = lambda.ParameterList.Parameters;
        if (parameters.Count == 0 || parameters.Count > 2) return null;

        var columnParameterName = parameters[0].Identifier.Text;
        string? valueParameterName = parameters.Count == 2 ? parameters[1].Identifier.Text : null;

        string? valueClrType = null;
        if (parameters.Count == 2 && parameters[1].Type is TypeSyntax declaredType)
        {
            valueClrType = ResolveTypeFromSyntax(declaredType, compilation, lambda.SyntaxTree);
        }

        var bodySource = ProfileLambdaQualifier.QualifyLambdaBody(lambda.Body, compilation);
        return new OverrideOperatorModel(
            Name: operatorName,
            ColumnParameterName: columnParameterName,
            ValueParameterName: valueParameterName,
            ValueClrType: valueClrType,
            PredicateBodyCSharp: bodySource,
            Location: declarationLocation);
    }

    private static OverrideOperatorModel? BuildOperatorFromSimpleLambda(
        string operatorName,
        SimpleLambdaExpressionSyntax lambda,
        Compilation? compilation,
        LocationInfo? declarationLocation)
    {
        var columnParameterName = lambda.Parameter.Identifier.Text;
        var bodySource = ProfileLambdaQualifier.QualifyLambdaBody(lambda.Body, compilation);
        return new OverrideOperatorModel(
            Name: operatorName,
            ColumnParameterName: columnParameterName,
            ValueParameterName: null,
            ValueClrType: null,
            PredicateBodyCSharp: bodySource,
            Location: declarationLocation);
    }

    private static string? ResolveTypeFromSyntax(TypeSyntax typeSyntax, Compilation? compilation, SyntaxTree syntaxTree)
    {
        if (compilation is null) return typeSyntax.ToString();
        SemanticModel? semanticModel;
        try { semanticModel = compilation.GetSemanticModel(syntaxTree); }
        catch (ArgumentException) { return typeSyntax.ToString(); }
        var typeInfo = semanticModel.GetTypeInfo(typeSyntax);
        if (typeInfo.Type is null) return typeSyntax.ToString();
        return ProfileLambdaQualifier.FormatType(typeInfo.Type);
    }

    private sealed record ChainInvocation(string MethodName, IReadOnlyList<ExpressionSyntax> Arguments, Location InvocationLocation);
}
