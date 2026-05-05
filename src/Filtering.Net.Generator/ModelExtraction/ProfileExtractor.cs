using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Filtering.Net.Generator;

/// <summary>
/// Extractor for the second pipeline branch: classes annotated with <c>[FilterProfile]</c>.
/// Walks <c>[FilterOperator]</c>-marked members, validates them, and returns a
/// <see cref="ProfileModel"/> + any diagnostics raised along the way.
/// Phase-6 diagnostics emitted from here: FN0011 (NonStaticOperator), FN0013 (InvalidBaseProfile),
/// FN1001 (DateTimeUtcNowInLambda), FN1007 (UntranslatableMethodInOperator).
/// </summary>
internal static class ProfileExtractor
{
    private const string FilterProfileAttributeFullName = "Filtering.Net.FilterProfileAttribute<T>";
    private const string FilterOperatorAttributeFullName = "Filtering.Net.FilterOperatorAttribute";

    /// <summary>Allow-list of method full names known to be EF-Core-translatable. Anything outside the list trips FN1007.</summary>
    private static readonly HashSet<string> EfTranslatableMethods = BuildTranslatableMethodSet();

    /// <summary>
    /// Member-access expressions that count as "DateTime / DateTimeOffset .UtcNow / .Now"
    /// for the FN1001 check. Comparing on the rendered "DateTime.UtcNow" string lets us
    /// catch both <c>DateTime.UtcNow</c> and <c>System.DateTime.UtcNow</c> (the latter via
    /// the symbol resolution path) without needing semantic info on every visit.
    /// </summary>
    private static readonly HashSet<string> ClockMemberDisplayNames = new(StringComparer.Ordinal)
    {
        "DateTime.UtcNow",
        "DateTime.Now",
        "DateTimeOffset.UtcNow",
        "DateTimeOffset.Now",
        "System.DateTime.UtcNow",
        "System.DateTime.Now",
        "System.DateTimeOffset.UtcNow",
        "System.DateTimeOffset.Now",
    };

    public static ProfileModelWithDiagnostics Extract(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var diagnostics = new List<DiagnosticInfo>();

        if (context.TargetSymbol is not INamedTypeSymbol profileSymbol)
        {
            return new ProfileModelWithDiagnostics(Model: null, Diagnostics: new EquatableList<DiagnosticInfo>(diagnostics));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var profileFullName = profileSymbol.ToDisplayString();
        var operatorNames = new List<string>();
        var seenOperatorNames = new HashSet<string>(StringComparer.Ordinal);

        // When the consuming project references Microsoft.EntityFrameworkCore, treat every method
        // on the EF.Functions extension surface as translatable. Without this, custom EF.Functions
        // extensions (e.g., npgsql's TrigramsAreSimilar) would trip FN1007.
        var efIsReferenced = IsEntityFrameworkCoreReferenced(context.SemanticModel.Compilation);

        // FN0013: BasedOn must itself carry [FilterProfile].
        var profileAttribute = context.Attributes.FirstOrDefault();
        var hasBasedOn = profileAttribute is not null && HasBasedOnNamedArg(profileAttribute);
        if (profileAttribute is not null)
        {
            ValidateBasedOnNamedArg(profileSymbol, profileAttribute, diagnostics);
        }

        foreach (var member in profileSymbol.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (member is not IPropertySymbol && member is not IMethodSymbol) continue;

            var operatorAttribute = FindFilterOperatorAttribute(member.GetAttributes());
            if (operatorAttribute is null) continue;

            // FN0011: [FilterOperator] must be on a public static member.
            if (member.DeclaredAccessibility != Accessibility.Public || !member.IsStatic)
            {
                diagnostics.Add(DiagnosticInfo.From(
                    DiagnosticDescriptors.NonStaticOperator,
                    member.Locations.FirstOrDefault(),
                    $"{profileFullName}.{member.Name}"));
            }

            if (operatorAttribute.ConstructorArguments.Length > 0
                && operatorAttribute.ConstructorArguments[0].Value is string operatorName)
            {
                if (seenOperatorNames.Add(operatorName))
                {
                    operatorNames.Add(operatorName);
                }
                else
                {
                    // FN0017: same operator name declared more than once on this profile.
                    // The first occurrence is silent; subsequent duplicates fire on their own location.
                    diagnostics.Add(DiagnosticInfo.From(
                        DiagnosticDescriptors.DuplicateOperatorOnProfile,
                        member.Locations.FirstOrDefault(),
                        operatorName,
                        profileFullName));
                }
            }

            // Lambda-body scans (FN1001 + FN1007) walk the operator's body syntax.
            ScanOperatorBody(member, diagnostics, cancellationToken, efIsReferenced);
        }

        // FN0016: a standalone profile (no BasedOn) must declare its own extractor method(s)
        // for the operator shapes it uses. Profiles that delegate to a base via BasedOn are
        // exempted — the base profile's own FN0016 check covers that side.
        if (!hasBasedOn && operatorNames.Count > 0)
        {
            ReportMissingExtractors(profileSymbol, profileFullName, operatorNames, diagnostics);
        }

        var model = new ProfileModel(
            ProfileFullName: profileFullName,
            OperatorNames: new EquatableList<string>(operatorNames),
            Location: LocationInfo.FromLocation(profileSymbol.Locations.FirstOrDefault()));

        return new ProfileModelWithDiagnostics(
            Model: model,
            Diagnostics: new EquatableList<DiagnosticInfo>(diagnostics));
    }

    /// <summary>True when the [FilterProfile&lt;T&gt;] attribute carries a <c>BasedOn = typeof(...)</c>
    /// named argument with a non-null type value.</summary>
    private static bool HasBasedOnNamedArg(AttributeData profileAttribute)
    {
        foreach (var namedArgument in profileAttribute.NamedArguments)
        {
            if (namedArgument.Key != "BasedOn") continue;
            if (namedArgument.Value.Value is INamedTypeSymbol) return true;
        }
        return false;
    }

    /// <summary>FN0016: ensure the profile declares <c>TryGetValue</c> when any scalar-shape
    /// operator is configured, and <c>TryGetArray</c> when the <c>in</c> operator is configured.
    /// Methods are looked up by name + public-static accessibility; signature-mismatch errors
    /// surface in consumer compilation.</summary>
    private static void ReportMissingExtractors(
        INamedTypeSymbol profileSymbol,
        string profileFullName,
        IReadOnlyList<string> operatorNames,
        List<DiagnosticInfo> diagnostics)
    {
        var hasScalarOperator = false;
        var hasArrayOperator = false;
        foreach (var operatorName in operatorNames)
        {
            if (operatorName == "isNull") continue;
            if (operatorName == "in") hasArrayOperator = true;
            else hasScalarOperator = true;
        }

        var missingMethods = new List<string>();
        if (hasScalarOperator && !ProfileDeclaresPublicStaticMethod(profileSymbol, "TryGetValue"))
        {
            missingMethods.Add("TryGetValue");
        }
        if (hasArrayOperator && !ProfileDeclaresPublicStaticMethod(profileSymbol, "TryGetArray"))
        {
            missingMethods.Add("TryGetArray");
        }
        if (missingMethods.Count == 0) return;

        diagnostics.Add(DiagnosticInfo.From(
            DiagnosticDescriptors.ProfileMissingExtractor,
            profileSymbol.Locations.FirstOrDefault(),
            profileFullName,
            string.Join(", ", missingMethods)));
    }

    private static bool ProfileDeclaresPublicStaticMethod(INamedTypeSymbol profileSymbol, string methodName)
    {
        foreach (var member in profileSymbol.GetMembers(methodName))
        {
            if (member is IMethodSymbol method
                && method.IsStatic
                && method.DeclaredAccessibility == Accessibility.Public)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>FN0013: emit if [FilterProfile(BasedOn = typeof(X))] X isn't itself marked [FilterProfile].</summary>
    private static void ValidateBasedOnNamedArg(
        INamedTypeSymbol profileSymbol,
        AttributeData profileAttribute,
        List<DiagnosticInfo> diagnostics)
    {
        foreach (var namedArgument in profileAttribute.NamedArguments)
        {
            if (namedArgument.Key != "BasedOn") continue;
            if (namedArgument.Value.Value is not INamedTypeSymbol basedOnType) continue;
            var basedOnHasProfileAttr = basedOnType.GetAttributes().Any(attributeData =>
                attributeData.AttributeClass?.OriginalDefinition?.ToDisplayString() == FilterProfileAttributeFullName);
            if (!basedOnHasProfileAttr)
            {
                diagnostics.Add(DiagnosticInfo.From(
                    DiagnosticDescriptors.InvalidBaseProfile,
                    profileSymbol.Locations.FirstOrDefault(),
                    basedOnType.ToDisplayString()));
            }
        }
    }

    private static AttributeData? FindFilterOperatorAttribute(System.Collections.Immutable.ImmutableArray<AttributeData> attributes)
    {
        foreach (var attributeData in attributes)
        {
            if (attributeData.AttributeClass?.ToDisplayString() == FilterOperatorAttributeFullName)
            {
                return attributeData;
            }
        }
        return null;
    }

    /// <summary>FN1001 + FN1007: walk the syntax of the operator member's body looking for
    /// clock members and untranslatable invocations.</summary>
    private static void ScanOperatorBody(
        ISymbol operatorMember,
        List<DiagnosticInfo> diagnostics,
        CancellationToken cancellationToken,
        bool efIsReferenced)
    {
        foreach (var syntaxReference in operatorMember.DeclaringSyntaxReferences)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var syntaxNode = syntaxReference.GetSyntax(cancellationToken);

            // FN1001: any DateTime/DateTimeOffset.UtcNow / .Now access inside the body.
            foreach (var memberAccess in syntaxNode.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
            {
                var rendered = memberAccess.ToString();
                if (ClockMemberDisplayNames.Contains(rendered))
                {
                    diagnostics.Add(DiagnosticInfo.From(
                        DiagnosticDescriptors.DateTimeUtcNowInLambda,
                        memberAccess.GetLocation()));
                    break; // one warning per operator is plenty.
                }
            }

            // FN1007: any invocation of a method not in the EF-translatable allow-list.
            foreach (var invocation in syntaxNode.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var (DisplayName, IsAllowed) = MatchAgainstAllowList(invocation.Expression, efIsReferenced);
                if (IsAllowed) continue;
                if (DisplayName is null) continue;
                diagnostics.Add(DiagnosticInfo.From(
                    DiagnosticDescriptors.UntranslatableMethodInOperator,
                    invocation.GetLocation(),
                    DisplayName));
            }
        }
    }

    /// <summary>
    /// Tries to match the invocation target syntax against the allow-list. We accept either:
    /// <list type="bullet">
    /// <item>The full rendering (e.g., <c>string.IsNullOrEmpty</c>, <c>EF.Functions.Like</c>) so dotted statics resolve.</item>
    /// <item>The right-most member name alone (e.g., <c>Contains</c> for <c>column.Contains</c>) so instance / extension calls on a runtime parameter resolve.</item>
    /// </list>
    /// When <paramref name="efIsReferenced"/> is true, any invocation rooted on
    /// <c>EF.Functions.*</c> is allowed unconditionally — the consuming project has EF Core in its
    /// graph, so even custom EF.Functions extensions (npgsql trigrams, full-text search helpers,
    /// etc.) translate.
    /// Returns the matched display name (when matched) and a flag.
    /// </summary>
    private static (string? DisplayName, bool IsAllowed) MatchAgainstAllowList(
        ExpressionSyntax invocationTarget,
        bool efIsReferenced)
    {
        switch (invocationTarget)
        {
            case MemberAccessExpressionSyntax memberAccess:
            {
                var fullRendering = memberAccess.ToString();
                if (EfTranslatableMethods.Contains(fullRendering))
                {
                    return (fullRendering, IsAllowed: true);
                }
                if (efIsReferenced && IsEfFunctionsAccess(memberAccess))
                {
                    return (fullRendering, IsAllowed: true);
                }
                var rightMostName = memberAccess.Name.Identifier.Text;
                if (EfTranslatableMethods.Contains(rightMostName))
                {
                    return (rightMostName, IsAllowed: true);
                }
                return (fullRendering, IsAllowed: false);
            }
            case IdentifierNameSyntax identifier:
            {
                var name = identifier.Identifier.Text;
                return (name, EfTranslatableMethods.Contains(name));
            }
            default:
                return (DisplayName: null, IsAllowed: true);
        }
    }

    /// <summary>
    /// True when the invocation target is rooted on <c>EF.Functions.*</c>. Detects both
    /// <c>EF.Functions.Foo(...)</c> and the rare <c>Microsoft.EntityFrameworkCore.EF.Functions.Foo(...)</c>.
    /// Walks the member-access spine until it reaches an <see cref="IdentifierNameSyntax"/>; the
    /// identifier preceding the right-most must be <c>Functions</c> and the one before that must
    /// end in <c>EF</c>.
    /// </summary>
    private static bool IsEfFunctionsAccess(MemberAccessExpressionSyntax memberAccess)
    {
        // Drill through Foo.Bar.Baz down to the leftmost identifier or "EF.Functions" sub-spine.
        var leftExpression = memberAccess.Expression;
        if (leftExpression is not MemberAccessExpressionSyntax leftMemberAccess) return false;
        if (leftMemberAccess.Name.Identifier.Text != "Functions") return false;

        // Now leftMemberAccess.Expression must end with the "EF" identifier.
        return leftMemberAccess.Expression switch
        {
            IdentifierNameSyntax leftIdentifier => leftIdentifier.Identifier.Text == "EF",
            MemberAccessExpressionSyntax deepMemberAccess => deepMemberAccess.Name.Identifier.Text == "EF",
            _ => false,
        };
    }

    /// <summary>True when the compilation references a Microsoft.EntityFrameworkCore* assembly.</summary>
    private static bool IsEntityFrameworkCoreReferenced(Compilation compilation)
    {
        foreach (var assemblyName in compilation.ReferencedAssemblyNames)
        {
            if (assemblyName.Name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Static allow-list of methods we know EF Core translates without client-eval. Includes
    /// instance method short-names (e.g., <c>Contains</c>) so syntactic matches like
    /// <c>column.Contains(value)</c> resolve correctly without semantic info.
    /// </summary>
    private static HashSet<string> BuildTranslatableMethodSet()
    {
        var set = new HashSet<string>(StringComparer.Ordinal)
        {
            // string instance methods (matched syntactically as <expr>.Method)
            "Contains", "StartsWith", "EndsWith", "ToLower", "ToUpper", "Trim", "Substring",
            // string statics
            "string.IsNullOrEmpty", "string.IsNullOrWhiteSpace", "string.Concat",
            "String.IsNullOrEmpty", "String.IsNullOrWhiteSpace", "String.Concat",
            // Enumerable / Queryable extensions (matched on short-name as <expr>.Any)
            "Any", "All", "Select", "Where", "Count", "FirstOrDefault", "Single", "SingleOrDefault",
            // Math
            "Math.Abs", "Math.Max", "Math.Min", "Math.Round", "Math.Floor", "Math.Ceiling",
            "Math.Pow", "Math.Sqrt", "Math.Sign",
            // ToString-like
            "ToString",
            // EF.Functions.* — pre-seed the most common entries so projects that *don't* reference
            // EF Core directly (rare, but possible) still get clean output. When EF is referenced,
            // the allow-list is broadened to every EF.Functions.* method via syntactic detection.
            "EF.Functions.Like", "EF.Functions.ILike", "EF.Functions.Collate",
        };
        return set;
    }
}
