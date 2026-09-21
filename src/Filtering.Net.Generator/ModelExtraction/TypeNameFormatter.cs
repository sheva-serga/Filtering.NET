using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator;

internal static class TypeNameFormatter
{
    // FullyQualifiedFormat drops nullable-reference annotations, which is what the FN1008 value-type
    // strings are matched against. A type that is spliced into generated code as a type argument
    // needs them back, or an invariant generic over TValue = string? stops matching the consumer's.
    private static readonly SymbolDisplayFormat NullableAnnotatedFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    // global::-qualified so the name resolves in generated code whatever usings the consumer has; keyword types stay keywords.
    public static string Format(ITypeSymbol typeSymbol) =>
        typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public static string FormatWithNullableAnnotations(ITypeSymbol typeSymbol) =>
        typeSymbol.ToDisplayString(NullableAnnotatedFormat);
}
