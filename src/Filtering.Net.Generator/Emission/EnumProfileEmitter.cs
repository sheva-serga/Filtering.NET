using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator;

/// <summary>Emits one <c>[FilterProfile&lt;TEnum&gt;]</c> static class per enum referenced by
/// any <c>[GenerateFilter&lt;TEntity&gt;]</c>'s filterable properties. The emitted class lives
/// under <c>Filtering.Net.Generated</c> and routes through <see cref="EnumExtractor"/>.</summary>
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
