using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Filtering.Net.Generator;

// Known limitation: the rewriter is parameter-name-based and doesn't track lexical scope.
// Nested lambdas that re-declare the outer column/value name would be incorrectly substituted.
// [FilterOperator] bodies are short LINQ expressions in practice, so this is accepted.
internal static class CustomOperatorEmitter
{
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
            // Rewrite receiver only — leaves member names (e.g., a property named "column") untouched.
            var rewrittenReceiver = (ExpressionSyntax)Visit(node.Expression);
            if (ReferenceEquals(rewrittenReceiver, node.Expression))
            {
                return node;
            }
            return node.WithExpression(rewrittenReceiver);
        }
    }
}
