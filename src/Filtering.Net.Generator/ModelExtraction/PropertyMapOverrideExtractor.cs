using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Filtering.Net.Generator;

/// <summary>
/// Extractor for the body of a <c>[PropertyMap]</c>-decorated method. Walks the user's
/// fluent <c>builder.For(...).Operator(...).Operator(...)</c> chain and pulls out the
/// property accessor + per-operator predicate metadata so the emitter can drop the
/// rewritten lambda bodies straight into typed leaf methods.
/// </summary>
internal static class PropertyMapOverrideExtractor
{
    /// <summary>
    /// Returns a populated <see cref="PropertyOverrideModel"/> for the given override method.
    /// Best-effort: if the body shape isn't the documented
    /// <c>builder.For(entity =&gt; entity.X).Operator(name, lambda).Operator(...)</c> chain,
    /// returns a model with empty <c>PropertyAccessorBodyCSharp</c> and no operators — the
    /// emitter then falls back to a throwing stub for that property.
    /// </summary>
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
            // Walk the invocation chain bottom-up: each .Operator(...) is the outermost
            // expression; descending through the .Expression of each MemberAccess gets us to
            // .For(...) at the innermost.
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

        // A [PropertyMap] override has a typed-value operator when at least one of its
        // .Operator(...) calls carries a typed value parameter (ValueClrType != null). Unary
        // override operators (column-only lambdas) have ValueClrType == null and leave this
        // flag false.
        var hasTypedValueOperator = operators.Exists(overrideOperator => overrideOperator.ValueClrType is not null);

        return new PropertyOverrideModel(
            PropertyName: propertyName,
            MethodName: methodSymbol.Name,
            PropertyAccessorBodyCSharp: accessorBody,
            EntityParameterName: entityParameterName,
            Operators: new EquatableList<OverrideOperatorModel>(operators),
            HasTypedValueOperator: hasTypedValueOperator);
    }

    /// <summary>
    /// Returns the expression returned by the override method — either the expression-body
    /// arrow, or the only return statement in the block body. Null when neither shape matches.
    /// </summary>
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

    /// <summary>Walks the dot-chain top-down and produces a list ordered from the innermost
    /// (<c>builder.For(...)</c>) to the outermost (<c>.Operator(name, …)</c>). Each entry
    /// carries the simple method name, the argument list, and the source location of the
    /// invocation so FN1008 can point at the declaration site.</summary>
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
        // builder.Operator("name", (a, b) => …)  → 2 args (name + lambda)
        // builder.Operator<T>("name", (a, b) => …) — same shape syntactically, but we look
        // at arguments only since type arguments are inferred from the lambda's signature.
        if (invocation.Arguments.Count != 2) return null;

        if (invocation.Arguments[0] is not LiteralExpressionSyntax nameLiteral) return null;
        var operatorName = nameLiteral.Token.ValueText;

        // The predicate is either a parenthesized lambda (the common case for two-parameter
        // (column, value) shape) or a simple lambda (single-parameter).
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

        // The TArgument type comes from the lambda parameter's declared type if present.
        // For overloads where the user writes typed parameters: (string tags, string value) =>
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

    /// <summary>Best-effort resolution of a type-syntax to its CLR display name via the
    /// semantic model. Falls back to the syntax text when no model is available.</summary>
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
