using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator;

internal static class EnumProfileEmitter
{
    public const string GeneratedNamespace = "Filtering.Net.Generated";

    public static string Emit(INamedTypeSymbol enumSymbol) =>
        ScribanRuntime.Render("EnumProfile", BuildView(enumSymbol));

    internal static EnumProfileView BuildView(INamedTypeSymbol enumSymbol) =>
        new(
            GeneratedNamespace: GeneratedNamespace,
            EnumFullName: "global::" + enumSymbol.ToDisplayString(),
            ClassName: enumSymbol.Name + "Filter");
}
