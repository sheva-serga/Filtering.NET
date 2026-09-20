using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator;

internal static class TypeNameFormatter
{
    // global::-qualified so the name resolves in generated code whatever usings the consumer has; keyword types stay keywords.
    public static string Format(ITypeSymbol typeSymbol) =>
        typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
}
