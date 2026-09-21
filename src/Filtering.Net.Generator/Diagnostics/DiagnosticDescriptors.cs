using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator;

internal static class DiagnosticDescriptors
{
    private const string Category = "Filtering.Net";

    // Single help-link target — every rule's "More info" goes to the catalogue table.
    private const string HelpLink = "https://sheva-serga.github.io/Filtering.NET/diagnostics/";

    // ---------- Errors (FN0001 - FN0029) ----------

    public static readonly DiagnosticDescriptor DuplicateMapping = new(
        id: "FN0001",
        title: "Duplicate filter mapping",
        messageFormat: "Filter path '{0}' is mapped by multiple sources on '{1}': {2}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Each effective dotted filter path must be produced by at most one mapping ([Map], [PropertyMap], or [MapNested]) on a given filter class.",
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor MapAndPropertyMapBoth = new(
        id: "FN0002",
        title: "Property has both [Map] and [PropertyMap]",
        messageFormat: "Property '{0}' has both a [Map] and a [PropertyMap]. Use one or the other.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor PropertyNotFound = new(
        id: "FN0003",
        title: "Property not found on entity",
        messageFormat: "Property '{0}' does not exist on entity type '{1}'.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor IncompatibleProfile = new(
        id: "FN0004",
        title: "Incompatible profile for property",
        messageFormat: "Profile '{0}' cannot be applied to property '{1}' of type '{2}'.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor UnknownOperator = new(
        id: "FN0005",
        title: "Unknown operator on profile",
        messageFormat: "Operator '{0}' is not declared by profile '{1}'.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor NoInferableProfile = new(
        id: "FN0006",
        title: "No inferable profile for property type",
        messageFormat: "Property '{0}' has CLR type '{1}' which has no built-in primitive profile. Specify Profile = typeof(...) explicitly.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor DuplicateInterceptor = new(
        id: "FN0007",
        title: "Duplicate value interceptor",
        messageFormat: "Property '{0}' has multiple [InterceptValue] declarations.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor NonStaticOperator = new(
        id: "FN0008",
        title: "[FilterOperator] member must be public static",
        messageFormat: "Member '{0}' has [FilterOperator] but is not public static.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor AliasCollision = new(
        id: "FN0009",
        title: "Alias collides with existing property or alias",
        messageFormat: "Alias '{0}' collides with another property or alias on entity '{1}' (case-insensitive).",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor InvalidBaseProfile = new(
        id: "FN0010",
        title: "[FilterProfile.BasedOn] references a non-profile type",
        messageFormat: "[FilterProfile(BasedOn = typeof({0}))] references a type that is not marked with [FilterProfile].",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor InterceptorWithoutMap = new(
        id: "FN0011",
        title: "Interceptor declared without matching [Map]",
        messageFormat: "Property '{0}' has [InterceptValue] but no [Map] declaration. Add a [Map] for this property.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor AmbiguousProfile = new(
        id: "FN0012",
        title: "Multiple filter profiles match property type",
        messageFormat: "Property '{0}' has CLR type '{1}' which is matched by multiple profiles ({2}). Use [Map(typeof(...))] on the property to pick one.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor DuplicateOperatorOnProfile = new(
        id: "FN0013",
        title: "Duplicate operator declaration on profile",
        messageFormat: "Operator '{0}' is declared more than once on profile '{1}'. Each operator name must appear at most once per profile.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor NestedCycle = new(
        id: "FN0014",
        title: "Cycle in [MapNested] graph",
        messageFormat: "Cycle detected in [MapNested] graph involving filter classes: {0}. Set MaxDepth on at least one [MapNested] in the cycle to allow it.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A [MapNested] cycle is only allowed when at least one nesting in it declares MaxDepth, which makes the expansion finite.",
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor NestedFilterClassUnusable = new(
        id: "FN0015",
        title: "[MapNested<T>] does not name a usable filter class for the navigation",
        messageFormat: "[MapNested<{0}>] on navigation '{1}' does not name a [GenerateFilter<{2}>] partial in this compilation. Name a filter class that targets '{2}', or drop the type argument to auto-resolve it; a filter class from another assembly cannot be nested in v1.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The explicit type argument must resolve to a [GenerateFilter<TNavigation>] partial in the same compilation: a type that is not a filter class, targets a different entity, or lives in a referenced assembly all reach this rule.",
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor NestedAmbiguous = new(
        id: "FN0016",
        title: "Auto-resolve [MapNested] is ambiguous",
        messageFormat: "[MapNested(nameof({0}))] is ambiguous: {1} candidate filter classes target '{2}'. Use the generic form [MapNested<TFilter>] to pick one.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor NestedTargetNotFound = new(
        id: "FN0017",
        title: "[MapNested] target filter class not found",
        messageFormat: "[MapNested(nameof({0}))] cannot resolve a filter class for '{1}': no [GenerateFilter<{1}>] partial in this compilation.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor NestedNavigationInvalid = new(
        id: "FN0018",
        title: "[MapNested] navigation property is not a single-target reference navigation",
        messageFormat: "[MapNested(nameof({0}))] target property does not exist on '{1}', is a primitive/value type, or is not a single-target reference navigation.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor NestedCollectionUnsupported = new(
        id: "FN0019",
        title: "[MapNested] on collection navigation is not supported in v1",
        messageFormat: "[MapNested(nameof({0}))] target is a collection navigation; collection navigations require Any/All quantifier semantics and are deferred to a future version.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor FilterClassHasBaseType = new(
        id: "FN0020",
        title: "[GenerateFilter] class declares a base class",
        messageFormat: "Filter class '{0}' derives from '{1}'. The generated part derives from FilterDefinition<TEntity>, so the class cannot declare another base class.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor NestedMaxDepthInvalid = new(
        id: "FN0021",
        title: "[MapNested] MaxDepth is out of range",
        messageFormat: "[MapNested(nameof({0}))] has MaxDepth = {1}. Use a value between 1 and {2} to bound the nesting, or omit it for an unbounded one.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "MaxDepth bounds how often one nesting may be followed along a single path. A negative value is meaningless, and a value above the supported ceiling would expand into an unusable number of filter paths.",
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor ProfileTypeNotAProfile = new(
        id: "FN0022",
        title: "[Map(Profile = ...)] references a non-profile type",
        messageFormat: "[Map(\"{0}\", Profile = typeof({1}))] references a type that is not marked with [FilterProfile<TColumn>]. Mark '{1}' with [FilterProfile<TColumn>] or point Profile at a profile class.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor PropertyMapSignatureInvalid = new(
        id: "FN0023",
        title: "[PropertyMap] method has an unusable signature",
        messageFormat: "[PropertyMap(\"{0}\")] method '{1}' cannot be called from generated code: {2}. Declare it as 'public static FilterRule<TEntity, TValue> {1}(FilterRuleBuilder<TEntity, TValue> builder)'.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A [PropertyMap] method whose signature the generated CreateSchema cannot call would silently drop the property from the schema.",
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor NullableValueTypeInPath = new(
        id: "FN0024",
        title: "Mapped path reads a member through a nullable value type",
        messageFormat: "Path '{0}' reads member '{1}' through nullable value type '{2}', which does not expose it. Map the member with a [PropertyMap] rule instead.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor NestedPrefixBlank = new(
        id: "FN0025",
        title: "[MapNested] Prefix is blank",
        messageFormat: "[MapNested(nameof({0}))] declares a blank Prefix. Omit Prefix to dispatch under the navigation name, or give it a non-blank value.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor NestedPathFilterUnknown = new(
        id: "FN0026",
        title: "[MapNested] Only/Except names a path the nested filter does not map",
        messageFormat: "[MapNested(nameof({0}))] {1} names '{2}', which the filter class '{3}' does not map. Nested paths contributed by a further [MapNested] must be spelled with a dot.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor FilterClassPlacementInvalid = new(
        id: "FN0027",
        title: "[GenerateFilter] class is nested or generic",
        messageFormat: "Filter class '{0}' cannot be generated because {1}. Declare [GenerateFilter<TEntity>] on a non-generic partial class that sits directly in a namespace.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The generated half of a filter class is emitted as a top-level partial in the class's namespace, so a nested or generic declaration would never be joined with it.",
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor OperatorMemberShapeInvalid = new(
        id: "FN0028",
        title: "[FilterOperator] member is not an operator template",
        messageFormat: "Member '{0}' declares operator '{1}' but its type is not Expression<Func<TColumn, bool>> or Expression<Func<TColumn, TValue, bool>>, so no operator can be built from it.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A [FilterOperator] member the generator cannot read a predicate shape from contributes nothing to the emitted profile, and requests naming that operator would be rejected as unknown.",
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor InterceptorSignatureInvalid = new(
        id: "FN0029",
        title: "[InterceptValue] method has an unusable signature",
        messageFormat: "[InterceptValue(\"{0}\")] method '{1}' cannot be called from generated code: {2}. Declare it as 'static TValue {1}(InterceptContext context, TValue value)'.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "An interceptor that is not static is spliced into the static CreateSchema as a method group and fails to compile; one with the wrong parameter shape is dropped and silently never runs.",
        helpLinkUri: HelpLink);

    // ---------- Warnings (FN1001 - FN1008) ----------

    public static readonly DiagnosticDescriptor DateTimeUtcNowInLambda = new(
        id: "FN1001",
        title: "DateTime.UtcNow/Now used directly inside [FilterOperator] lambda",
        messageFormat: "[FilterOperator] body references DateTime.UtcNow/Now directly inside the lambda. Consider using the method-shape override that pre-computes the cutoff for app-clock semantics.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor NotSortableLikelyOmission = new(
        id: "FN1002",
        title: "Property likely should be sortable",
        messageFormat: "Property '{0}' has CLR type '{1}' but is not marked Sortable=true. Did you forget to make it sortable?",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor ProfileUnused = new(
        id: "FN1003",
        title: "Filter profile is declared but unused",
        messageFormat: "Profile '{0}' is declared but never referenced by any [Map].",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor OperatorUnused = new(
        id: "FN1004",
        title: "Operator is declared but unused",
        messageFormat: "Operator '{0}' on profile '{1}' is declared but never referenced.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor ZeroOperatorsAllowed = new(
        id: "FN1005",
        title: "Property allows zero operators",
        messageFormat: "Property '{0}' allows zero operators (Only/Except excluded everything). Filter leaves on this field will always fail validation.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor NullableNavInPath = new(
        id: "FN1006",
        title: "Path crosses nullable navigation property",
        messageFormat: "Path '{0}' crosses a nullable navigation property. Consider explicit null handling in custom mapping.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor UntranslatableMethodInOperator = new(
        id: "FN1007",
        title: "Operator body uses untranslatable method",
        messageFormat: "Method '{0}' in operator body is not in the EF Core translatable allow-list. May produce client-side evaluation or runtime errors.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    // Fires only when the assembly is annotated with [assembly: FilterValueDiagnostics(WarnUnregistered = true)].
    public static readonly DiagnosticDescriptor FilterValueTypeUnregistered = new(
        id: "FN1008",
        title: "Filter value type is not registered in any visible JsonSerializerContext",
        messageFormat: "Filter value type '{0}' on '{1}' is not registered in any [JsonSerializable] attribute on a JsonSerializerContext visible in this compilation. Calls to JsonSerializer.Deserialize will fail under NativeAOT/trim unless reflection fallback is configured.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);
}
