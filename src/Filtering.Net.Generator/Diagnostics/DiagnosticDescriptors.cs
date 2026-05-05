using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator;

/// <summary>Central registry of every diagnostic the Filtering.Net source generator emits.</summary>
internal static class DiagnosticDescriptors
{
    private const string Category = "Filtering.Net";

    /// <summary>
    /// Base URI for per-diagnostic explainer markdown files. Resolves to a `master`-branch
    /// blob URL on GitHub so the link works for any consumer who hits a diagnostic.
    /// </summary>
    private const string HelpLinkBase = "https://github.com/sheva-serga/Filtering.Net/blob/master/docs/diagnostics/";

    private static string HelpLinkFor(string diagnosticId) => HelpLinkBase + diagnosticId + ".md";

    // ---------- Errors (FN0001 - FN0017) ----------

    public static readonly DiagnosticDescriptor DuplicateMap = new(
        id: "FN0001",
        title: "Duplicate filter mapping",
        messageFormat: "Property '{0}' is mapped by multiple methods. Each property must have at most one [Map] declaration.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkFor("FN0001"));

    public static readonly DiagnosticDescriptor DuplicateSortable = new(
        id: "FN0002",
        title: "Duplicate sortable mapping",
        messageFormat: "Property '{0}' is marked Sortable=true on multiple [Map] methods.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkFor("FN0002"));

    public static readonly DiagnosticDescriptor MapAndPropertyMapBoth = new(
        id: "FN0003",
        title: "Property has both [Map] and [PropertyMap]",
        messageFormat: "Property '{0}' has both a [Map] and a [PropertyMap]. Use one or the other.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkFor("FN0003"));

    public static readonly DiagnosticDescriptor PropertyNotFound = new(
        id: "FN0004",
        title: "Property not found on entity",
        messageFormat: "Property '{0}' does not exist on entity type '{1}'.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkFor("FN0004"));

    public static readonly DiagnosticDescriptor IncompatibleProfile = new(
        id: "FN0005",
        title: "Incompatible profile for property",
        messageFormat: "Profile '{0}' cannot be applied to property '{1}' of type '{2}'.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkFor("FN0005"));

    public static readonly DiagnosticDescriptor UnknownOperator = new(
        id: "FN0006",
        title: "Unknown operator on profile",
        messageFormat: "Operator '{0}' is not declared by profile '{1}'.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkFor("FN0006"));

    public static readonly DiagnosticDescriptor InvalidValueConverter = new(
        id: "FN0007",
        title: "Invalid value converter type",
        messageFormat: "Type '{0}' referenced by [ConvertWith] does not inherit from Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<TModel, TProvider>.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkFor("FN0007"));

    public static readonly DiagnosticDescriptor MissingPartial = new(
        id: "FN0008",
        title: "[Map] method must be partial",
        messageFormat: "Method '{0}' has [Map] but is not declared 'partial'. The source generator can only emit the implementation for partial methods.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkFor("FN0008"));

    public static readonly DiagnosticDescriptor NoInferableProfile = new(
        id: "FN0009",
        title: "No inferable profile for property type",
        messageFormat: "Property '{0}' has CLR type '{1}' which has no built-in primitive profile. Specify Profile = typeof(...) explicitly.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkFor("FN0009"));

    public static readonly DiagnosticDescriptor DuplicateInterceptor = new(
        id: "FN0010",
        title: "Duplicate value interceptor",
        messageFormat: "Property '{0}' has multiple [InterceptValue] declarations.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkFor("FN0010"));

    public static readonly DiagnosticDescriptor NonStaticOperator = new(
        id: "FN0011",
        title: "[FilterOperator] member must be public static",
        messageFormat: "Member '{0}' has [FilterOperator] but is not public static.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkFor("FN0011"));

    public static readonly DiagnosticDescriptor AliasCollision = new(
        id: "FN0012",
        title: "Alias collides with existing property or alias",
        messageFormat: "Alias '{0}' collides with another property or alias on entity '{1}' (case-insensitive).",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkFor("FN0012"));

    public static readonly DiagnosticDescriptor InvalidBaseProfile = new(
        id: "FN0013",
        title: "[FilterProfile.BasedOn] references a non-profile type",
        messageFormat: "[FilterProfile(BasedOn = typeof({0}))] references a type that is not marked with [FilterProfile].",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkFor("FN0013"));

    public static readonly DiagnosticDescriptor InterceptorWithoutMap = new(
        id: "FN0014",
        title: "Interceptor declared without matching [Map]",
        messageFormat: "Property '{0}' has [InterceptValue] but no [Map] declaration. Add a [Map] for this property.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkFor("FN0014"));

    public static readonly DiagnosticDescriptor AmbiguousProfile = new(
        id: "FN0015",
        title: "Multiple filter profiles match property type",
        messageFormat: "Property '{0}' has CLR type '{1}' which is matched by multiple profiles ({2}). Use [Map(typeof(...))] on the property to pick one.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkFor("FN0015"));

    public static readonly DiagnosticDescriptor ProfileMissingExtractor = new(
        id: "FN0016",
        title: "Standalone filter profile is missing a required extractor method",
        messageFormat: "Profile '{0}' has no [FilterProfile.BasedOn] and is missing required extractor method(s): {1}. Either declare these public static methods on the profile or set BasedOn = typeof(...) to inherit them from a profile that does.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkFor("FN0016"));

    public static readonly DiagnosticDescriptor DuplicateOperatorOnProfile = new(
        id: "FN0017",
        title: "Duplicate operator declaration on profile",
        messageFormat: "Operator '{0}' is declared more than once on profile '{1}'. Each operator name must appear at most once per profile.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkFor("FN0017"));

    // ---------- Warnings (FN1001 - FN1008) ----------

    public static readonly DiagnosticDescriptor DateTimeUtcNowInLambda = new(
        id: "FN1001",
        title: "DateTime.UtcNow/Now used directly inside [FilterOperator] lambda",
        messageFormat: "[FilterOperator] body references DateTime.UtcNow/Now directly inside the lambda. Consider using the method-shape override that pre-computes the cutoff for app-clock semantics.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkFor("FN1001"));

    public static readonly DiagnosticDescriptor NotSortableLikelyOmission = new(
        id: "FN1002",
        title: "Property likely should be sortable",
        messageFormat: "Property '{0}' has CLR type '{1}' but is not marked Sortable=true. Did you forget to make it sortable?",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkFor("FN1002"));

    public static readonly DiagnosticDescriptor ProfileUnused = new(
        id: "FN1003",
        title: "Filter profile is declared but unused",
        messageFormat: "Profile '{0}' is declared but never referenced by any [Map].",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkFor("FN1003"));

    public static readonly DiagnosticDescriptor OperatorUnused = new(
        id: "FN1004",
        title: "Operator is declared but unused",
        messageFormat: "Operator '{0}' on profile '{1}' is declared but never referenced.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkFor("FN1004"));

    public static readonly DiagnosticDescriptor ZeroOperatorsAllowed = new(
        id: "FN1005",
        title: "Property allows zero operators",
        messageFormat: "Property '{0}' allows zero operators (Only/Except excluded everything). Filter leaves on this field will always fail validation.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkFor("FN1005"));

    public static readonly DiagnosticDescriptor NullableNavInPath = new(
        id: "FN1006",
        title: "Path crosses nullable navigation property",
        messageFormat: "Path '{0}' crosses a nullable navigation property. Consider explicit null handling in custom mapping.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkFor("FN1006"));

    public static readonly DiagnosticDescriptor UntranslatableMethodInOperator = new(
        id: "FN1007",
        title: "Operator body uses untranslatable method",
        messageFormat: "Method '{0}' in operator body is not in the EF Core translatable allow-list. May produce client-side evaluation or runtime errors.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkFor("FN1007"));

    public static readonly DiagnosticDescriptor FilterValueTypeUnregistered = new(
        id: "FN1008",
        title: "Filter value type is not registered in any visible JsonSerializerContext",
        messageFormat: "Filter value type '{0}' on '{1}' is not registered in any [JsonSerializable] attribute on a JsonSerializerContext visible in this compilation. Calls to JsonSerializer.Deserialize will fail under NativeAOT/trim unless reflection fallback is configured.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Fires only when the assembly is annotated with [assembly: FilterValueDiagnostics(WarnUnregistered = true)]. Add [JsonSerializable(typeof(T))] to a visible JsonSerializerContext, or remove the opt-in attribute if reflection-based deserialization is acceptable.",
        helpLinkUri: HelpLinkFor("FN1008"));
}
