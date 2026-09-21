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

    private const string AutoEmittedEnumProfileNamespace = "Filtering.Net.Generated";

    public static bool IsAutoEmittedEnumProfile(string profileFullName) =>
        profileFullName.StartsWith(AutoEmittedEnumProfileNamespace + ".", StringComparison.Ordinal);

    // Walks the BasedOn chain so a derived profile inherits base operators; same-named derived
    // operators win. Cycle protection: short-circuits when a profile is seen a second time.
    public static ResolvedProfile ResolveExplicit(INamedTypeSymbol profileType)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        // Operator name -> declaring profile full name; derived-profile overwrites win.
        var operatorDeclarers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        // Operator name -> custom-operator metadata (only present when the declaring member
        // is on a non-built-in profile and the lambda body could be extracted).
        var operatorMetadata = new Dictionary<string, CustomOperatorModel>(StringComparer.OrdinalIgnoreCase);
        // Insertion order preserves the user's declaration order in the snapshot output.
        var operatorOrder = new List<string>();

        CollectOperatorsRecursive(profileType, visited, operatorDeclarers, operatorMetadata, operatorOrder);

        var profileFullName = profileType.ToDisplayString();
        var customOperators = operatorOrder
            .Where(name => operatorMetadata.ContainsKey(name))
            .Select(name => operatorMetadata[name])
            .ToList();
        return new ResolvedProfile(profileFullName, operatorOrder, customOperators);
    }

    private static void CollectOperatorsRecursive(
        INamedTypeSymbol profileType,
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
                CollectOperatorsRecursive(basedOnType, visited, operatorDeclarers, operatorMetadata, operatorOrder);
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

                // A member with no readable predicate shape reaches no FilterOperator entry in the
                // emitted bridge, so it must not be advertised here either — that is FN0028.
                var predicateSignature = OperatorPredicateSignature.TryRead(member);
                if (predicateSignature is null) continue;

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

                operatorMetadata[operatorName] = new CustomOperatorModel(
                    OperatorName: operatorName,
                    DeclaringProfileFullName: profileFullName,
                    ValueClrType: predicateSignature.ValueType is null ? null : TypeNameFormatter.Format(predicateSignature.ValueType),
                    Location: LocationInfo.FromLocation(member.Locations.FirstOrDefault()));
            }
        }
    }

    public static bool IsBuiltInProfileName(string profileFullName) =>
        profileFullName.StartsWith("Filtering.Net.", StringComparison.Ordinal)
        && !profileFullName.StartsWith("Filtering.Net.Generated.", StringComparison.Ordinal);

    // clrTypeKey is the property's leaf CLR type with Nullable<T> already unwrapped, spelled the way
    // ITypeSymbol.ToDisplayString() spells it ("string", "int", "System.Guid").
    public static bool IsCompatible(string clrTypeKey, string profileFullName)
    {
        return profileFullName switch
        {
            StringFilterFullName => clrTypeKey == "string",
            BoolFilterFullName => clrTypeKey == "bool",
            GuidFilterFullName => clrTypeKey == "System.Guid",
            Int32FilterFullName => clrTypeKey == "int",
            Int64FilterFullName => clrTypeKey == "long",
            Int16FilterFullName => clrTypeKey == "short",
            ByteFilterFullName => clrTypeKey == "byte",
            DecimalFilterFullName => clrTypeKey == "decimal",
            DoubleFilterFullName => clrTypeKey == "double",
            SingleFilterFullName => clrTypeKey == "float",
            DateTimeFilterFullName => clrTypeKey == "System.DateTime",
            DateTimeOffsetFilterFullName => clrTypeKey == "System.DateTimeOffset",
            DateOnlyFilterFullName => clrTypeKey == "System.DateOnly",
            TimeOnlyFilterFullName => clrTypeKey == "System.TimeOnly",
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
