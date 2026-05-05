using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Filtering.Net.Generator;

/// <summary>
/// Shared lambda-body qualifier used by both <see cref="ProfileResolver"/> (for custom
/// <c>[FilterOperator]</c> bodies) and <see cref="PropertyMapOverrideExtractor"/> (for
/// <c>[PropertyMap]</c> override bodies). Walks the supplied syntax tree and replaces
/// type identifiers with their fully-qualified <c>global::Namespace.Type</c> form via
/// the semantic model so the rewritten body resolves inside the generated file
/// regardless of which usings the consumer brings.
/// </summary>
internal static class ProfileLambdaQualifier
{
    /// <summary>Qualify all type identifiers in the supplied lambda body. Returns the raw
    /// source when no compilation / semantic model is available, or when the syntax tree
    /// isn't owned by the supplied compilation (defensive — happens in some test paths).</summary>
    public static string QualifyLambdaBody(CSharpSyntaxNode lambdaBody, Compilation? compilation)
    {
        if (compilation is null) return lambdaBody.ToString();
        SemanticModel? semanticModel;
        try
        {
            semanticModel = compilation.GetSemanticModel(lambdaBody.SyntaxTree);
        }
        catch (ArgumentException)
        {
            return lambdaBody.ToString();
        }
        var rewriter = new TypeIdentifierQualifier(semanticModel);
        var rewritten = rewriter.Visit(lambdaBody);
        return rewritten.ToFullString();
    }

    /// <summary>
    /// Renders a CLR type as it should appear in emitted code. C# language keyword types
    /// (<c>string</c>, <c>int</c>, …) skip the <c>global::</c> prefix because the compiler
    /// rejects <c>global::string</c>; everything else gets prefixed for unambiguous binding.
    /// Arrays / nullables recurse through their element / underlying type.
    /// </summary>
    public static string FormatType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is IArrayTypeSymbol arrayType)
        {
            return FormatType(arrayType.ElementType) + "[]";
        }
        if (typeSymbol is INamedTypeSymbol named
            && named.IsGenericType
            && named.ConstructedFrom?.SpecialType == SpecialType.System_Nullable_T)
        {
            return FormatType(named.TypeArguments[0]) + "?";
        }

        if (IsBuiltInKeywordType(typeSymbol))
        {
            return typeSymbol.ToDisplayString();
        }
        return "global::" + typeSymbol.ToDisplayString();
    }

    private static bool IsBuiltInKeywordType(ITypeSymbol typeSymbol)
    {
        return typeSymbol.SpecialType is
            SpecialType.System_String
            or SpecialType.System_Boolean
            or SpecialType.System_Char
            or SpecialType.System_Byte
            or SpecialType.System_SByte
            or SpecialType.System_Int16
            or SpecialType.System_UInt16
            or SpecialType.System_Int32
            or SpecialType.System_UInt32
            or SpecialType.System_Int64
            or SpecialType.System_UInt64
            or SpecialType.System_Single
            or SpecialType.System_Double
            or SpecialType.System_Decimal
            or SpecialType.System_Object;
    }

    /// <summary>Rewriter that prepends <c>global::</c> + namespace to type identifiers so
    /// the rewritten body resolves without any usings in the generated file.</summary>
    private sealed class TypeIdentifierQualifier(SemanticModel semanticModel) : CSharpSyntaxRewriter
    {
        private readonly SemanticModel _semanticModel = semanticModel;

        public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            // Walk the receiver first; the right-hand member name doesn't qualify on its own.
            var rewrittenReceiver = (ExpressionSyntax)Visit(node.Expression);
            if (ReferenceEquals(rewrittenReceiver, node.Expression)) return node;
            return node.WithExpression(rewrittenReceiver);
        }

        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            // Skip names that are part of a qualified expression — we'd double-qualify.
            if (node.Parent is QualifiedNameSyntax qualified && qualified.Right == node) return node;
            if (node.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == node) return node;
            if (node.Parent is AliasQualifiedNameSyntax) return node;

            var symbolInfo = _semanticModel.GetSymbolInfo(node);
            if (symbolInfo.Symbol is not INamedTypeSymbol typeSymbol) return node;
            if (typeSymbol.ContainingNamespace is null || typeSymbol.ContainingNamespace.IsGlobalNamespace)
            {
                return SyntaxFactory.ParseExpression("global::" + typeSymbol.Name)
                    .WithLeadingTrivia(node.GetLeadingTrivia())
                    .WithTrailingTrivia(node.GetTrailingTrivia());
            }
            var fullyQualified = "global::" + typeSymbol.ContainingNamespace.ToDisplayString() + "." + typeSymbol.Name;
            return SyntaxFactory.ParseExpression(fullyQualified)
                .WithLeadingTrivia(node.GetLeadingTrivia())
                .WithTrailingTrivia(node.GetTrailingTrivia());
        }
    }
}
