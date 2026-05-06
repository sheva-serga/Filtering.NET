using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Filtering.Net.Generator;

internal static class ProfileExtractor
{
    private const string FilterProfileAttributeFullName = "Filtering.Net.FilterProfileAttribute<T>";
    private const string FilterOperatorAttributeFullName = "Filtering.Net.FilterOperatorAttribute";

    private static readonly HashSet<string> EfTranslatableMethods = BuildTranslatableMethodSet();

    // Rendered display names for the FN1001 check — catches both short and fully-qualified forms
    // without requiring semantic info on every visit.
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

        // When EF Core is referenced, any EF.Functions.* call is translatable — including custom
        // extensions like npgsql's TrigramsAreSimilar that aren't in the static allow-list.
        var efIsReferenced = IsEntityFrameworkCoreReferenced(context.SemanticModel.Compilation);

        // FN0012: BasedOn must itself carry [FilterProfile].
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

            // FN0010: [FilterOperator] must be on a public static member.
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
                    // FN0016: first occurrence is silent; subsequent duplicates fire on their location.
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

        // FN0015: standalone profiles (no BasedOn) must own their extractor methods;
        // profiles with BasedOn delegate to the base, which is checked separately.
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

    private static bool HasBasedOnNamedArg(AttributeData profileAttribute)
    {
        foreach (var namedArgument in profileAttribute.NamedArguments)
        {
            if (namedArgument.Key != "BasedOn") continue;
            if (namedArgument.Value.Value is INamedTypeSymbol) return true;
        }
        return false;
    }

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

    // Detects EF.Functions.Foo(...) and Microsoft.EntityFrameworkCore.EF.Functions.Foo(...).
    private static bool IsEfFunctionsAccess(MemberAccessExpressionSyntax memberAccess)
    {
        var leftExpression = memberAccess.Expression;
        if (leftExpression is not MemberAccessExpressionSyntax leftMemberAccess) return false;
        if (leftMemberAccess.Name.Identifier.Text != "Functions") return false;

        return leftMemberAccess.Expression switch
        {
            IdentifierNameSyntax leftIdentifier => leftIdentifier.Identifier.Text == "EF",
            MemberAccessExpressionSyntax deepMemberAccess => deepMemberAccess.Name.Identifier.Text == "EF",
            _ => false,
        };
    }

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

    // Short names (e.g., "Contains") match instance/extension calls on runtime parameters
    // without needing semantic info.
    private static HashSet<string> BuildTranslatableMethodSet()
    {
        var set = new HashSet<string>(StringComparer.Ordinal)
        {
            "Contains", "StartsWith", "EndsWith", "ToLower", "ToUpper", "Trim", "Substring",
            "string.IsNullOrEmpty", "string.IsNullOrWhiteSpace", "string.Concat",
            "String.IsNullOrEmpty", "String.IsNullOrWhiteSpace", "String.Concat",
            "Any", "All", "Select", "Where", "Count", "FirstOrDefault", "Single", "SingleOrDefault",
            "Math.Abs", "Math.Max", "Math.Min", "Math.Round", "Math.Floor", "Math.Ceiling",
            "Math.Pow", "Math.Sqrt", "Math.Sign",
            "ToString",
            // Pre-seed common entries; when EF Core is referenced the allow-list broadens to
            // all EF.Functions.* via syntactic detection in IsEfFunctionsAccess.
            "EF.Functions.Like", "EF.Functions.ILike", "EF.Functions.Collate",
        };
        return set;
    }
}
