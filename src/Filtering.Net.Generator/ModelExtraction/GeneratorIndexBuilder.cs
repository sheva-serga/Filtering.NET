using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator;

// Builds the one compilation-wide index every filter class reads. Runs once per compilation in its
// own pipeline node; nothing below it touches the Compilation, so per-class extraction is a pure
// function of its own syntax tree plus this value-equatable index.
internal static class GeneratorIndexBuilder
{
    private const string FilterDefaultsAttributeFullName = "Filtering.Net.FilterDefaultsAttribute";
    private const string FilterValueDiagnosticsAttributeFullName = "Filtering.Net.FilterValueDiagnosticsAttribute";

    private static readonly string[] BuiltInEnumOperators = ["eq", "ne", "in", "isNull"];

    public static GeneratorIndex Build(Compilation compilation, CancellationToken cancellationToken)
    {
        var enumTypes = EnumTypeCollector.Collect(compilation);
        cancellationToken.ThrowIfCancellationRequested();

        var enumProfiles = BuildEnumProfileDescriptors(enumTypes);

        var profileBindings = ProfileIndexBuilder.CollectProfileTypes(compilation);
        cancellationToken.ThrowIfCancellationRequested();

        var profilesByClrType = ProfileIndexBuilder.BuildFrom(profileBindings, enumProfiles);

        var profileDefinitions = new List<ProfileDefinition>(profileBindings.Count + enumTypes.Count);
        var alreadyDefined = new HashSet<string>(StringComparer.Ordinal);
        foreach (var binding in profileBindings)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var profileFullName = binding.ProfileType.ToDisplayString();
            if (!alreadyDefined.Add(profileFullName)) continue;

            var resolvedProfile = ProfileResolver.ResolveExplicit(binding.ProfileType);
            profileDefinitions.Add(new ProfileDefinition(
                ProfileFullName: profileFullName,
                ColumnTypeKey: binding.ClrTypeKey,
                Operators: new EquatableList<string>(resolvedProfile.Operators),
                CustomOperators: new EquatableList<CustomOperatorModel>(resolvedProfile.CustomOperators),
                Bridges: new EquatableList<ProfileBridgeModel>(ProfileBridgeBuilder.BuildChain(binding.ProfileType)),
                DeclarationLocation: LocationInfo.FromLocation(binding.ProfileType.Locations.FirstOrDefault())));
        }

        foreach (var enumProfile in enumProfiles)
        {
            if (!alreadyDefined.Add(enumProfile.ProfileFullName)) continue;
            profileDefinitions.Add(new ProfileDefinition(
                ProfileFullName: enumProfile.ProfileFullName,
                ColumnTypeKey: enumProfile.EnumFullName,
                Operators: new EquatableList<string>(BuiltInEnumOperators),
                CustomOperators: new EquatableList<CustomOperatorModel>(),
                Bridges: new EquatableList<ProfileBridgeModel>(),
                DeclarationLocation: null));
        }

        cancellationToken.ThrowIfCancellationRequested();

        // FN1008 is opt-in, so the JsonSerializerContext walk only happens for assemblies that
        // asked for it — and it happens here, in the one node that already holds the Compilation.
        var isFilterValueDiagnosticsOptIn = IsWarnUnregisteredOptIn(compilation.Assembly);
        var registeredJsonTypeNames = isFilterValueDiagnosticsOptIn
            ? JsonSerializableTypeCollector.CollectRegisteredTypeNames(compilation)
            : [];

        return new GeneratorIndex(
            assemblyDefaults: ReadAssemblyDefaults(compilation.Assembly),
            profilesByClrType: profilesByClrType,
            profiles: new EquatableList<ProfileDefinition>(profileDefinitions),
            enumProfiles: new EquatableList<EnumProfileDescriptor>(enumProfiles),
            isDependencyInjectionReferenced: DiExtensionEmitter.IsDiAbstractionsReferenced(compilation),
            isFilterValueDiagnosticsOptIn: isFilterValueDiagnosticsOptIn,
            registeredJsonTypeNames: new EquatableList<string>(registeredJsonTypeNames));
    }

    // Two enums with the same simple name in different namespaces (or nested in different types)
    // would otherwise share a generated class name, a profile full name and an AddSource hint name
    // — the last of which fails the whole generator run. The short name is kept while it is unique
    // in the compilation; every enum in a colliding group falls back to its mangled full name.
    private static List<EnumProfileDescriptor> BuildEnumProfileDescriptors(IReadOnlyList<INamedTypeSymbol> enumTypes)
    {
        var simpleNameCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var enumType in enumTypes)
        {
            simpleNameCounts.TryGetValue(enumType.Name, out var occurrences);
            simpleNameCounts[enumType.Name] = occurrences + 1;
        }

        var descriptors = new List<EnumProfileDescriptor>(enumTypes.Count);
        foreach (var enumType in enumTypes)
        {
            var enumFullName = enumType.ToDisplayString();
            var className = simpleNameCounts[enumType.Name] == 1
                ? enumType.Name + "Filter"
                : EmissionNames.ToIdentifier(enumFullName) + "Filter";
            descriptors.Add(new EnumProfileDescriptor(
                EnumFullName: enumFullName,
                ProfileFullName: $"{EnumProfileEmitter.GeneratedNamespace}.{className}",
                ClassName: className));
        }
        return descriptors;
    }

    private static bool IsWarnUnregisteredOptIn(IAssemblySymbol assemblySymbol)
    {
        foreach (var assemblyAttribute in assemblySymbol.GetAttributes())
        {
            if (assemblyAttribute.AttributeClass?.ToDisplayString() != FilterValueDiagnosticsAttributeFullName) continue;
            foreach (var namedArgument in assemblyAttribute.NamedArguments)
            {
                if (namedArgument.Key == "WarnUnregistered" && namedArgument.Value.Value is bool warnUnregistered)
                {
                    return warnUnregistered;
                }
            }
        }
        return false;
    }

    private static AssemblyFilterDefaults ReadAssemblyDefaults(IAssemblySymbol assemblySymbol)
    {
        int? defaultPageSize = null;
        int? maxPageSize = null;
        int? maxNestingDepth = null;
        int? maxLeafConditions = null;

        foreach (var assemblyAttribute in assemblySymbol.GetAttributes())
        {
            if (assemblyAttribute.AttributeClass?.ToDisplayString() != FilterDefaultsAttributeFullName) continue;
            foreach (var namedArgument in assemblyAttribute.NamedArguments)
            {
                if (namedArgument.Value.Value is not int configuredValue) continue;
                switch (namedArgument.Key)
                {
                    case "DefaultPageSize":
                        defaultPageSize = configuredValue;
                        break;
                    case "MaxPageSize":
                        maxPageSize = configuredValue;
                        break;
                    case "MaxNestingDepth":
                        maxNestingDepth = configuredValue;
                        break;
                    case "MaxLeafConditions":
                        maxLeafConditions = configuredValue;
                        break;
                }
            }
        }

        return new AssemblyFilterDefaults(defaultPageSize, maxPageSize, maxNestingDepth, maxLeafConditions);
    }
}
