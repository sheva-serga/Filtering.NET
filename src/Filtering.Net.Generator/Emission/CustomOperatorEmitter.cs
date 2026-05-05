using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Filtering.Net.Generator;

/// <summary>
/// Emitter for custom <c>[FilterOperator]</c> lambda bodies. Rewrites the column parameter
/// to the property accessor and the value parameter to the leaf method's value variable,
/// then emits the rewritten body as the typed leaf method's expression body.
/// </summary>
/// <remarks>
/// Known limitation (v1, accepted by design): the rewriter is parameter-name-based and does
/// not track lexical scope. A nested lambda inside a custom operator that re-declares a
/// parameter with the same name as the outer column or value parameter would be incorrectly
/// substituted. In practice <c>[FilterOperator]</c> bodies are short LINQ-style expressions
/// where this doesn't occur, but the limitation is documented here so a future iteration
/// can lift it by walking <c>SimpleLambdaExpressionSyntax</c>/<c>ParenthesizedLambdaExpressionSyntax</c>
/// and pushing/popping a shadow set during traversal.
/// </remarks>
internal static class CustomOperatorEmitter
{
    /// <summary>Rewrites the lambda body source so the column parameter becomes the property
    /// accessor expression and the value parameter becomes the supplied value-variable name.
    /// Returns the rewritten C# source ready to drop into a <c>=&gt; entity =&gt; …</c> tail.</summary>
    public static string RewriteLambdaBody(
        string lambdaBodySource,
        string columnParameterName,
        string columnReplacement,
        string? valueParameterName,
        string? valueReplacement)
    {
        // Parse the body as an expression (the common case) or fall back to a block parse.
        var bodyExpression = SyntaxFactory.ParseExpression(lambdaBodySource);
        if (!bodyExpression.ContainsDiagnostics)
        {
            var rewrittenExpression = (ExpressionSyntax)new IdentifierRewriter(
                columnParameterName, columnReplacement, valueParameterName, valueReplacement)
                .Visit(bodyExpression);
            return rewrittenExpression.ToFullString();
        }

        var bodyStatement = SyntaxFactory.ParseStatement(lambdaBodySource);
        var rewrittenStatement = new IdentifierRewriter(
            columnParameterName, columnReplacement, valueParameterName, valueReplacement)
            .Visit(bodyStatement);
        return rewrittenStatement.ToFullString();
    }

    /// <summary>
    /// Rewriter that replaces bare <see cref="IdentifierNameSyntax"/> occurrences of the
    /// column / value parameter names with their target replacement source. Member-access
    /// right-hand sides are left alone (e.g., <c>foo.column</c> where <c>column</c> is a
    /// property name remains untouched) by overriding <see cref="VisitMemberAccessExpression"/>
    /// to recurse only into the receiver.
    /// </summary>
    private sealed class IdentifierRewriter(
        string columnName,
        string columnReplacement,
        string? valueName,
        string? valueReplacement) : CSharpSyntaxRewriter
    {
        private readonly string _columnName = columnName;
        private readonly string _columnReplacement = columnReplacement;
        private readonly string? _valueName = valueName;
        private readonly string? _valueReplacement = valueReplacement;

        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            var identifierText = node.Identifier.Text;
            if (identifierText == _columnName)
            {
                return SyntaxFactory.ParseExpression(_columnReplacement)
                    .WithLeadingTrivia(node.GetLeadingTrivia())
                    .WithTrailingTrivia(node.GetTrailingTrivia());
            }
            if (_valueName is not null && _valueReplacement is not null && identifierText == _valueName)
            {
                return SyntaxFactory.ParseExpression(_valueReplacement)
                    .WithLeadingTrivia(node.GetLeadingTrivia())
                    .WithTrailingTrivia(node.GetTrailingTrivia());
            }
            return base.VisitIdentifierName(node);
        }

        public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            // Rewrite the receiver but leave the member name alone (so a property named
            // "column" on some receiver doesn't get rewritten).
            var rewrittenReceiver = (ExpressionSyntax)Visit(node.Expression);
            if (ReferenceEquals(rewrittenReceiver, node.Expression))
            {
                return node;
            }
            return node.WithExpression(rewrittenReceiver);
        }
    }
}
