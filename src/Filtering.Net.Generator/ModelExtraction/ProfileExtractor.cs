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
        // Tracks first-occurrence location per operator name so a subsequent duplicate diagnostic
        // can point at the original declaration via additionalLocations. Case-insensitive because
        // FilterProfile<TColumn> keys its operator dictionary that way, so two names differing only
        // in case are one operator at runtime.
        var firstOperatorLocation = new Dictionary<string, Location?>(StringComparer.OrdinalIgnoreCase);

        // When EF Core is referenced, any EF.Functions.* call is translatable — including custom
        // extensions like npgsql's TrigramsAreSimilar that aren't in the static allow-list.
        var efIsReferenced = IsEntityFrameworkCoreReferenced(context.SemanticModel.Compilation);

        // FN0010: BasedOn must itself carry [FilterProfile].
        var profileAttribute = context.Attributes.FirstOrDefault();
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

            // FN0008: [FilterOperator] must be on a public static member.
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
                var memberLocation = member.Locations.FirstOrDefault();

                // FN0028: a member the generator cannot read a predicate shape from contributes no
                // FilterOperator entry, so it would otherwise be advertised by AllowedOperators and
                // then rejected at request time as an unknown operator.
                if (OperatorPredicateSignature.TryRead(member) is null)
                {
                    diagnostics.Add(DiagnosticInfo.From(
                        DiagnosticDescriptors.OperatorMemberShapeInvalid,
                        memberLocation,
                        $"{profileFullName}.{member.Name}",
                        operatorName));
                    ScanOperatorBody(member, diagnostics, cancellationToken, efIsReferenced);
                    continue;
                }

                if (!firstOperatorLocation.ContainsKey(operatorName))
                {
                    operatorNames.Add(operatorName);
                    firstOperatorLocation[operatorName] = memberLocation;
                }
                else
                {
                    var firstLocation = firstOperatorLocation[operatorName];
                    var additionalLocations = firstLocation is not null
                        ? new[] { firstLocation }
                        : Array.Empty<Location>();
                    diagnostics.Add(DiagnosticInfo.From(
                        DiagnosticDescriptors.DuplicateOperatorOnProfile,
                        memberLocation,
                        additionalLocations,
                        operatorName,
                        profileFullName));
                }
            }

            // Lambda-body scans (FN1001 + FN1007) walk the operator's body syntax.
            ScanOperatorBody(member, diagnostics, cancellationToken, efIsReferenced);
        }

        var model = new ProfileModel(
            ProfileFullName: profileFullName,
            OperatorNames: new EquatableList<string>(operatorNames),
            Location: LocationInfo.FromLocation(profileSymbol.Locations.FirstOrDefault()));

        return new ProfileModelWithDiagnostics(
            Model: model,
            Diagnostics: new EquatableList<DiagnosticInfo>(diagnostics));
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
                var basedOnLocation = basedOnType.Locations.FirstOrDefault();
                var additionalLocations = basedOnLocation is not null && basedOnLocation != Location.None
                    ? new[] { basedOnLocation }
                    : Array.Empty<Location>();
                diagnostics.Add(DiagnosticInfo.From(
                    DiagnosticDescriptors.InvalidBaseProfile,
                    profileSymbol.Locations.FirstOrDefault(),
                    additionalLocations,
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
