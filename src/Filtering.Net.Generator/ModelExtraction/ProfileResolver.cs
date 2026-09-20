using Microsoft.CodeAnalysis;

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
                    // Built-in operators come from the profile's own runtime Profile accessor.
                    operatorMetadata.Remove(operatorName);
                    continue;
                }

                var customMetadata = TryBuildCustomOperatorModel(member, operatorName, profileFullName);
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

    public static bool IsBuiltInProfileName(string profileFullName) =>
        profileFullName.StartsWith("Filtering.Net.", StringComparison.Ordinal)
        && !profileFullName.StartsWith("Filtering.Net.Generated.", StringComparison.Ordinal);

    // Returns null when the member is not an Expression<Func<...>> of a supported arity; the operator
    // then has no runtime bridge entry and surfaces as a configuration error at startup.
    private static CustomOperatorModel? TryBuildCustomOperatorModel(
        ISymbol operatorMember,
        string operatorName,
        string declaringProfileFullName)
    {
        var predicateSignature = OperatorPredicateSignature.TryRead(operatorMember);
        if (predicateSignature is null) return null;

        return new CustomOperatorModel(
            OperatorName: operatorName,
            DeclaringProfileFullName: declaringProfileFullName,
            ValueClrType: predicateSignature.ValueType is null ? null : TypeNameFormatter.Format(predicateSignature.ValueType),
            Location: LocationInfo.FromLocation(operatorMember.Locations.FirstOrDefault()));
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
