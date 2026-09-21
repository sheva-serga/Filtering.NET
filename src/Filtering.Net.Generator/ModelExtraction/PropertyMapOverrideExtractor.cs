using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Filtering.Net.Generator;

// Reads the .Operator(...) calls of a [PropertyMap] method for diagnostics and typed-value detection.
// The method itself runs at filter construction, so bodies are never parsed for emission.
internal static class PropertyMapOverrideExtractor
{
    private const string FilterRuleTypeName = "FilterRule";
    private const string FilterRuleBuilderTypeName = "FilterRuleBuilder";
    private const string FilteringNetNamespace = "Filtering.Net";
    private const string OperatorMethodName = "Operator";

    public static PropertyOverrideModel Extract(
        IMethodSymbol methodSymbol,
        string propertyName,
        LocationInfo? declarationLocation,
        SemanticModel? hostSemanticModel)
    {
        var operators = CollectOperators(methodSymbol, hostSemanticModel);
        var hasTypedValueOperator = operators.Exists(overrideOperator => overrideOperator.ValueClrType is not null);
        var signatureProblem = DescribeSignatureProblem(methodSymbol);

        return new PropertyOverrideModel(
            PropertyName: propertyName,
            MethodName: methodSymbol.Name,
            BuilderTypeFqn: signatureProblem is null
                ? TypeNameFormatter.FormatWithNullableAnnotations(methodSymbol.Parameters[0].Type)
                : null,
            Operators: new EquatableList<OverrideOperatorModel>(operators),
            HasTypedValueOperator: hasTypedValueOperator,
            SignatureProblem: signatureProblem,
            DeclarationLocation: declarationLocation);
    }

    // Returns null when the generated CreateSchema can call the method, else why it cannot.
    private static string? DescribeSignatureProblem(IMethodSymbol methodSymbol)
    {
        if (!methodSymbol.IsStatic) return "it is an instance method";
        if (methodSymbol.Parameters.Length != 1)
        {
            return $"it takes {methodSymbol.Parameters.Length} parameters instead of exactly one FilterRuleBuilder<TEntity, TValue>";
        }
        if (!IsFilteringNetGenericType(methodSymbol.ReturnType, FilterRuleTypeName))
        {
            return $"it returns '{methodSymbol.ReturnType.ToDisplayString()}' instead of FilterRule<TEntity, TValue>";
        }
        if (!IsFilteringNetGenericType(methodSymbol.Parameters[0].Type, FilterRuleBuilderTypeName))
        {
            return $"its parameter is '{methodSymbol.Parameters[0].Type.ToDisplayString()}' instead of FilterRuleBuilder<TEntity, TValue>";
        }
        return null;
    }

    // Matched on namespace + metadata name, so a consumer type that happens to be called FilterRule
    // cannot pass the check and produce a compile error inside generated code.
    private static bool IsFilteringNetGenericType(ITypeSymbol type, string typeName) =>
        type is INamedTypeSymbol { TypeArguments.Length: 2 } namedType
        && namedType.Name == typeName
        && namedType.ContainingNamespace?.ToDisplayString() == FilteringNetNamespace;

    // The whole method body is scanned rather than the returned expression's member-access chain:
    // a statement-bodied rule, a rule built up over several statements, or a wrapped chain are all
    // legal, and missing their operators would silently drop the JSON-resolver constructor pair.
    private static List<OverrideOperatorModel> CollectOperators(IMethodSymbol methodSymbol, SemanticModel? hostSemanticModel)
    {
        var operators = new List<OverrideOperatorModel>();
        foreach (var syntaxReference in methodSymbol.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax() is not MethodDeclarationSyntax methodDeclaration) continue;
            var semanticModel = ResolveSemanticModel(hostSemanticModel, methodDeclaration.SyntaxTree);

            var operatorInvocations = new List<(int Position, InvocationExpressionSyntax Invocation)>();
            foreach (var invocation in methodDeclaration.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess) continue;
                if (memberAccess.Name.Identifier.Text != OperatorMethodName) continue;
                if (!IsRuleBuilderOperator(invocation, semanticModel)) continue;
                operatorInvocations.Add((memberAccess.Name.SpanStart, invocation));
            }

            // Descendant order runs outside-in through a fluent chain; the member name's position
            // restores the order the consumer wrote the operators in.
            operatorInvocations.Sort((left, right) => left.Position.CompareTo(right.Position));
            foreach (var (_, invocation) in operatorInvocations)
            {
                var operatorModel = TryExtractOperator(invocation, semanticModel);
                if (operatorModel is not null) operators.Add(operatorModel);
            }
        }
        return operators;
    }

    // An Operator call whose value lambda is untyped cannot bind, so an unresolved symbol counts as
    // a match; only a call that binds to something other than FilterRuleBuilder is rejected.
    private static bool IsRuleBuilderOperator(InvocationExpressionSyntax invocation, SemanticModel? semanticModel)
    {
        if (semanticModel is null) return true;
        if (semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol operatorMethod) return true;
        return operatorMethod.ContainingType is INamedTypeSymbol containingType
            && containingType.Name == FilterRuleBuilderTypeName
            && containingType.ContainingNamespace?.ToDisplayString() == FilteringNetNamespace;
    }

    // The driver-provided model covers the filter class's own tree; a [PropertyMap] method declared
    // in another partial file needs its own, which is the only case that pays for a fresh model.
    private static SemanticModel? ResolveSemanticModel(SemanticModel? hostSemanticModel, SyntaxTree syntaxTree)
    {
        if (hostSemanticModel is null) return null;
        if (hostSemanticModel.SyntaxTree == syntaxTree) return hostSemanticModel;
        try { return hostSemanticModel.Compilation.GetSemanticModel(syntaxTree); }
        catch (ArgumentException) { return null; }
    }

    private static OverrideOperatorModel? TryExtractOperator(InvocationExpressionSyntax invocation, SemanticModel? semanticModel)
    {
        var arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count != 2) return null;
        if (arguments[0].Expression is not LiteralExpressionSyntax nameLiteral) return null;

        return new OverrideOperatorModel(
            Name: nameLiteral.Token.ValueText,
            ValueClrType: ResolveValueClrType(invocation, arguments[1].Expression, semanticModel),
            Location: LocationInfo.FromLocation(invocation.GetLocation()));
    }

    // The lambda's arity decides unary vs value, because it is reliable even when the call fails to
    // bind (an untyped two-parameter lambda cannot infer TArgument). The type then comes from the
    // bound Operator<TArgument> when available, else from the lambda's declared parameter type.
    private static string? ResolveValueClrType(InvocationExpressionSyntax invocation, ExpressionSyntax predicate, SemanticModel? semanticModel)
    {
        var boundTypeArguments = semanticModel?.GetSymbolInfo(invocation).Symbol is IMethodSymbol operatorMethod
            ? operatorMethod.TypeArguments
            : default;

        if (predicate is ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters.Count: 2 } valueLambda)
        {
            if (!boundTypeArguments.IsDefaultOrEmpty) return TypeNameFormatter.Format(boundTypeArguments[0]);
            var declaredValueType = valueLambda.ParameterList.Parameters[1].Type;
            if (declaredValueType is null) return "object";
            return semanticModel?.GetTypeInfo(declaredValueType).Type is { } resolvedValueType
                ? TypeNameFormatter.Format(resolvedValueType)
                : declaredValueType.ToString();
        }
        if (predicate is LambdaExpressionSyntax) return null;

        return boundTypeArguments.IsDefaultOrEmpty ? null : TypeNameFormatter.Format(boundTypeArguments[0]);
    }
}
