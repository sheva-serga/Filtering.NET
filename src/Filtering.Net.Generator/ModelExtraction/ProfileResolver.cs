using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Filtering.Net.Generator;

internal static class ProfileResolver
{
    public const string StringFilterFullName = "Filtering.Net.StringFilter";
    public const string DateTimeFilterFullName = "Filtering.Net.DateTimeFilter";
    public const string DateTimeOffsetFilterFullName = "Filtering.Net.DateTimeOffsetFilter";
    public const string DateOnlyFilterFullName = "Filtering.Net.DateOnlyFilter";
    public const string TimeOnlyFilterFullName = "Filtering.Net.TimeOnlyFilter";
    public const string BoolFilterFullName = "Filtering.Net.BoolFilter";
    public const string GuidFilterFullName = "Filtering.Net.GuidFilter";
    public const string Int32FilterFullName = "Filtering.Net.Int32Filter";
    public const string Int64FilterFullName = "Filtering.Net.Int64Filter";
    public const string Int16FilterFullName = "Filtering.Net.Int16Filter";
    public const string ByteFilterFullName = "Filtering.Net.ByteFilter";
    public const string DecimalFilterFullName = "Filtering.Net.DecimalFilter";
    public const string DoubleFilterFullName = "Filtering.Net.DoubleFilter";
    public const string SingleFilterFullName = "Filtering.Net.SingleFilter";

    private const string FilterOperatorAttributeFullName = "Filtering.Net.FilterOperatorAttribute";
    private const string FilterProfileAttributeFullName = "Filtering.Net.FilterProfileAttribute<T>";

    private static readonly IReadOnlyList<CustomOperatorModel> EmptyCustomOperators = [];

    private const string AutoEmittedEnumProfileNamespace = "Filtering.Net.Generated";

    public static ResolvedProfile? TryBuildVirtualEnumProfile(string profileFullName, ITypeSymbol propertyType)
    {
        if (!profileFullName.StartsWith(AutoEmittedEnumProfileNamespace + ".", StringComparison.Ordinal))
        {
            return null;
        }
        var unwrapped = UnwrapNullable(propertyType);
        if (unwrapped.TypeKind != TypeKind.Enum)
        {
            return null;
        }
        return new ResolvedProfile(profileFullName, BuiltInEnumOperators, EmptyCustomOperators);
    }

    public static bool IsAutoEmittedEnumProfile(string profileFullName) =>
        profileFullName.StartsWith(AutoEmittedEnumProfileNamespace + ".", StringComparison.Ordinal);

    // Custom user-defined profiles do not own extractors; they delegate to their BasedOn root.
    public static bool ProfileOwnsExtractor(string profileFullName) =>
        IsBuiltInProfileName(profileFullName) || IsAutoEmittedEnumProfile(profileFullName);

    // Returns the profile's own full name when it owns an extractor, or when no extractor-owning
    // ancestor is found (defensive fallback; consumer compile errors surface the misconfiguration).
    public static string ResolveExtractorProfileFullName(INamedTypeSymbol profileType)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = profileType;
        while (current is not null)
        {
            var fullName = current.ToDisplayString();
            if (!visited.Add(fullName)) break; // cycle guard
            if (ProfileOwnsExtractor(fullName)) return fullName;

            INamedTypeSymbol? next = null;
            foreach (var attributeData in current.GetAttributes())
            {
                if (attributeData.AttributeClass?.OriginalDefinition?.ToDisplayString() != FilterProfileAttributeFullName) continue;
                foreach (var namedArgument in attributeData.NamedArguments)
                {
                    if (namedArgument.Key != "BasedOn") continue;
                    if (namedArgument.Value.Value is INamedTypeSymbol basedOnType)
                    {
                        next = basedOnType;
                    }
                }
            }
            if (next is null) break;
            current = next;
        }
        return profileType.ToDisplayString();
    }

    // Walks the BasedOn chain so a derived profile inherits base operators; same-named derived
    // operators win. Cycle protection: short-circuits when a profile is seen a second time.
    public static ResolvedProfile? ResolveExplicit(INamedTypeSymbol profileType, Compilation? compilation = null)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        // Operator name -> declaring profile full name; derived-profile overwrites win.
        var operatorDeclarers = new Dictionary<string, string>(StringComparer.Ordinal);
        // Operator name -> custom-operator metadata (only present when the declaring member
        // is on a non-built-in profile and the lambda body could be extracted).
        var operatorMetadata = new Dictionary<string, CustomOperatorModel>(StringComparer.Ordinal);
        // Insertion order preserves the user's declaration order in the snapshot output.
        var operatorOrder = new List<string>();

        CollectOperatorsRecursive(profileType, compilation, visited, operatorDeclarers, operatorMetadata, operatorOrder);

        var profileFullName = profileType.ToDisplayString();
        var customOperators = operatorOrder
            .Where(name => operatorMetadata.ContainsKey(name))
            .Select(name => operatorMetadata[name])
            .ToList();
        return new ResolvedProfile(profileFullName, operatorOrder, customOperators);
    }

    private static void CollectOperatorsRecursive(
        INamedTypeSymbol profileType,
        Compilation? compilation,
        HashSet<string> visited,
        Dictionary<string, string> operatorDeclarers,
        Dictionary<string, CustomOperatorModel> operatorMetadata,
        List<string> operatorOrder)
    {
        var profileFullName = profileType.ToDisplayString();
        if (!visited.Add(profileFullName)) return; // cycle guard

        var isBuiltIn = IsBuiltInProfileName(profileFullName);

        // Recurse into BasedOn first so the derived profile's same-named operator wins.
        foreach (var attributeData in profileType.GetAttributes())
        {
            if (attributeData.AttributeClass?.OriginalDefinition?.ToDisplayString() != FilterProfileAttributeFullName) continue;
            foreach (var namedArgument in attributeData.NamedArguments)
            {
                if (namedArgument.Key != "BasedOn") continue;
                if (namedArgument.Value.Value is not INamedTypeSymbol basedOnType) continue;
                CollectOperatorsRecursive(basedOnType, compilation, visited, operatorDeclarers, operatorMetadata, operatorOrder);
            }
        }

        foreach (var member in profileType.GetMembers())
        {
            // [FilterOperator] is allowed on properties and methods.
            if (member is not IPropertySymbol && member is not IMethodSymbol) continue;
            foreach (var attributeData in member.GetAttributes())
            {
                if (attributeData.AttributeClass?.ToDisplayString() != FilterOperatorAttributeFullName) continue;
                if (attributeData.ConstructorArguments.Length == 0) continue;
                if (attributeData.ConstructorArguments[0].Value is not string operatorName) continue;

                if (!operatorDeclarers.ContainsKey(operatorName))
                {
                    operatorOrder.Add(operatorName);
                }
                operatorDeclarers[operatorName] = profileFullName;

                if (isBuiltIn)
                {
                    // Built-in operators emit via BuiltInProfileCatalog; no lambda metadata needed.
                    operatorMetadata.Remove(operatorName);
                    continue;
                }

                var customMetadata = TryBuildCustomOperatorModel(member, operatorName, profileFullName, compilation);
                if (customMetadata is not null)
                {
                    operatorMetadata[operatorName] = customMetadata;
                }
                else
                {
                    operatorMetadata.Remove(operatorName);
                }
            }
        }
    }

    // Mirrors BuiltInProfileCatalog.IsBuiltIn but keeps the resolver independent of emission.
    private static bool IsBuiltInProfileName(string profileFullName) =>
        profileFullName.StartsWith("Filtering.Net.", StringComparison.Ordinal)
        && !profileFullName.StartsWith("Filtering.Net.Generated.", StringComparison.Ordinal);

    // Returns null for unsupported shapes (statement-bodied methods, Expression.Lambda factory, etc.);
    // the emitter then falls back to a throwing stub.
    private static CustomOperatorModel? TryBuildCustomOperatorModel(
        ISymbol operatorMember,
        string operatorName,
        string declaringProfileFullName,
        Compilation? compilation)
    {
        var lambdaSyntax = TryFindLambdaSyntax(operatorMember);
        if (lambdaSyntax is null) return null;

        var parameters = lambdaSyntax.ParameterList.Parameters;
        if (parameters.Count == 0 || parameters.Count > 2) return null;

        var columnParameterName = parameters[0].Identifier.Text;
        string? valueParameterName = null;
        string? valueClrType = null;
        var isArrayValue = false;

        if (parameters.Count == 2)
        {
            valueParameterName = parameters[1].Identifier.Text;
            var valueType = ExtractValueTypeFromExpressionFunc(operatorMember, parameterIndex: 1);
            if (valueType is not null)
            {
                valueClrType = ProfileLambdaQualifier.FormatType(valueType);
                isArrayValue = valueType is IArrayTypeSymbol;
            }
        }

        // Identifiers are fully qualified so the body resolves in the generated file without
        // any using directives. Falls back to raw source when no compilation is available.
        var lambdaBodySource = ProfileLambdaQualifier.QualifyLambdaBody(lambdaSyntax.Body, compilation);

        return new CustomOperatorModel(
            OperatorName: operatorName,
            DeclaringProfileFullName: declaringProfileFullName,
            ColumnParameterName: columnParameterName,
            ValueParameterName: valueParameterName,
            ValueClrType: valueClrType,
            IsArrayValue: isArrayValue,
            LambdaBodyCSharp: lambdaBodySource,
            Location: LocationInfo.FromLocation(operatorMember.Locations.FirstOrDefault()));
    }


    private static ParenthesizedLambdaExpressionSyntax? TryFindLambdaSyntax(ISymbol operatorMember)
    {
        foreach (var syntaxReference in operatorMember.DeclaringSyntaxReferences)
        {
            var syntax = syntaxReference.GetSyntax();
            ArrowExpressionClauseSyntax? arrow = syntax switch
            {
                PropertyDeclarationSyntax property => property.ExpressionBody,
                MethodDeclarationSyntax method => method.ExpressionBody,
                _ => null,
            };
            if (arrow is not null && arrow.Expression is ParenthesizedLambdaExpressionSyntax arrowLambda)
            {
                return arrowLambda;
            }

            if (syntax is PropertyDeclarationSyntax blockProperty && blockProperty.AccessorList is not null)
            {
                foreach (var accessor in blockProperty.AccessorList.Accessors)
                {
                    if (!accessor.IsKind(SyntaxKind.GetAccessorDeclaration)) continue;
                    if (accessor.ExpressionBody?.Expression is ParenthesizedLambdaExpressionSyntax accessorArrowLambda)
                    {
                        return accessorArrowLambda;
                    }
                    if (accessor.Body is null) continue;
                    foreach (var statement in accessor.Body.Statements)
                    {
                        if (statement is ReturnStatementSyntax returnStatement
                            && returnStatement.Expression is ParenthesizedLambdaExpressionSyntax returnLambda)
                        {
                            return returnLambda;
                        }
                    }
                }
            }

            if (syntax is MethodDeclarationSyntax blockMethod && blockMethod.Body is not null)
            {
                foreach (var statement in blockMethod.Body.Statements)
                {
                    if (statement is ReturnStatementSyntax returnStatement
                        && returnStatement.Expression is ParenthesizedLambdaExpressionSyntax methodReturnLambda)
                    {
                        return methodReturnLambda;
                    }
                }
            }
        }
        return null;
    }

    private static ITypeSymbol? ExtractValueTypeFromExpressionFunc(ISymbol operatorMember, int parameterIndex)
    {
        ITypeSymbol? memberType = operatorMember switch
        {
            IPropertySymbol property => property.Type,
            IMethodSymbol method => method.ReturnType,
            _ => null,
        };
        if (memberType is not INamedTypeSymbol expressionType) return null;
        if (expressionType.TypeArguments.Length != 1) return null;
        if (expressionType.TypeArguments[0] is not INamedTypeSymbol funcType) return null;
        if (parameterIndex >= funcType.TypeArguments.Length - 1) return null;
        return funcType.TypeArguments[parameterIndex];
    }

    public static bool IsCompatible(ITypeSymbol clrType, string profileFullName)
    {
        var unwrapped = UnwrapNullable(clrType);
        return profileFullName switch
        {
            StringFilterFullName => unwrapped.SpecialType == SpecialType.System_String,
            BoolFilterFullName => unwrapped.SpecialType == SpecialType.System_Boolean,
            GuidFilterFullName => unwrapped.ToDisplayString() == "System.Guid",
            Int32FilterFullName => unwrapped.SpecialType == SpecialType.System_Int32,
            Int64FilterFullName => unwrapped.SpecialType == SpecialType.System_Int64,
            Int16FilterFullName => unwrapped.SpecialType == SpecialType.System_Int16,
            ByteFilterFullName => unwrapped.SpecialType == SpecialType.System_Byte,
            DecimalFilterFullName => unwrapped.SpecialType == SpecialType.System_Decimal,
            DoubleFilterFullName => unwrapped.SpecialType == SpecialType.System_Double,
            SingleFilterFullName => unwrapped.SpecialType == SpecialType.System_Single,
            DateTimeFilterFullName => unwrapped.ToDisplayString() == "System.DateTime",
            DateTimeOffsetFilterFullName => unwrapped.ToDisplayString() == "System.DateTimeOffset",
            DateOnlyFilterFullName => unwrapped.ToDisplayString() == "System.DateOnly",
            TimeOnlyFilterFullName => unwrapped.ToDisplayString() == "System.TimeOnly",
            _ => true,// Custom profiles: leave compatibility validation to the user.
        };
    }

    private static ITypeSymbol UnwrapNullable(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType
            && namedType.IsGenericType
            && namedType.ConstructedFrom?.SpecialType == SpecialType.System_Nullable_T)
        {
            return namedType.TypeArguments[0];
        }
        return type;
    }

    private static readonly string[] BuiltInEnumOperators =
        ["eq", "ne", "in", "isNull"];

    public static ResolvedProfileCandidates ResolveCandidates(ITypeSymbol clrType, ProfileIndex index)
    {
        var unwrapped = UnwrapNullable(clrType);
        var candidates = index.Lookup(unwrapped.ToDisplayString());
        return new ResolvedProfileCandidates(candidates);
    }

    public readonly struct ResolvedProfileCandidates(IReadOnlyList<string> profileFullNames)
    {
        public IReadOnlyList<string> ProfileFullNames { get; } = profileFullNames;

        public int Count => ProfileFullNames.Count;
    }
}
