using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator;

// Describes the runtime FilterProfile<T> instances the generator must emit for user-declared profiles.
// Built-in and auto-emitted enum profiles expose their own Profile accessor and need no bridge.
internal static class ProfileBridgeBuilder
{
    public const string GeneratedNamespace = "Filtering.Net.Generated";

    private const string FilterOperatorAttributeFullName = "Filtering.Net.FilterOperatorAttribute";
    private const string FilterProfileAttributeFullName = "Filtering.Net.FilterProfileAttribute<T>";
    private const string OperatorFactoryType = "global::Filtering.Net.FilterOperator";

    public static string ProfileReference(string profileFullName) =>
        ProfileResolver.IsBuiltInProfileName(profileFullName) || ProfileResolver.IsAutoEmittedEnumProfile(profileFullName)
            ? $"global::{profileFullName}.Profile"
            : $"global::{GeneratedNamespace}.{BridgeClassName(profileFullName)}.Instance";

    // Base profiles come first so the emitted static initialisers read top-down.
    public static List<ProfileBridgeModel> BuildChain(INamedTypeSymbol profileType)
    {
        var bridges = new List<ProfileBridgeModel>();
        var visitedProfiles = new HashSet<string>(StringComparer.Ordinal);
        AppendChain(profileType, bridges, visitedProfiles);
        return bridges;
    }

    private static void AppendChain(INamedTypeSymbol profileType, List<ProfileBridgeModel> bridges, HashSet<string> visitedProfiles)
    {
        var profileFullName = profileType.ToDisplayString();
        if (!visitedProfiles.Add(profileFullName)) return;
        if (ProfileResolver.IsBuiltInProfileName(profileFullName) || ProfileResolver.IsAutoEmittedEnumProfile(profileFullName)) return;

        ITypeSymbol? columnType = null;
        INamedTypeSymbol? basedOnType = null;
        foreach (var attributeData in profileType.GetAttributes())
        {
            var attributeClass = attributeData.AttributeClass;
            if (attributeClass?.OriginalDefinition?.ToDisplayString() != FilterProfileAttributeFullName) continue;
            if (attributeClass.TypeArguments.Length == 1) columnType = attributeClass.TypeArguments[0];
            foreach (var namedArgument in attributeData.NamedArguments)
            {
                if (namedArgument.Key == "BasedOn" && namedArgument.Value.Value is INamedTypeSymbol basedOnSymbol)
                {
                    basedOnType = basedOnSymbol;
                }
            }
        }
        if (columnType is null) return;

        if (basedOnType is not null) AppendChain(basedOnType, bridges, visitedProfiles);

        var profileTypeFqn = TypeNameFormatter.Format(profileType);
        var operators = new List<ProfileBridgeOperatorModel>();
        foreach (var member in profileType.GetMembers())
        {
            var operatorName = ReadOperatorName(member);
            if (operatorName is null) continue;
            var predicateSignature = OperatorPredicateSignature.TryRead(member);
            if (predicateSignature is null) continue;

            var memberAccess = member is IMethodSymbol ? $"{profileTypeFqn}.{member.Name}()" : $"{profileTypeFqn}.{member.Name}";
            operators.Add(new ProfileBridgeOperatorModel(operatorName, BuildFactoryCall(operatorName, predicateSignature, memberAccess)));
        }

        bridges.Add(new ProfileBridgeModel(
            ProfileFullName: profileFullName,
            ProfileName: profileType.Name,
            BridgeClassName: BridgeClassName(profileFullName),
            ColumnTypeFqn: TypeNameFormatter.Format(columnType),
            BaseProfileReference: basedOnType is null ? null : ProfileReference(basedOnType.ToDisplayString()),
            Operators: new EquatableList<ProfileBridgeOperatorModel>(operators)));
    }

    private static string BuildFactoryCall(string operatorName, OperatorPredicateSignature predicateSignature, string memberAccess)
    {
        var quotedName = "\"" + EmissionNames.EscapeStringLiteral(operatorName) + "\"";
        if (predicateSignature.ValueType is not null)
        {
            // Value operators declared on user profiles always take the typed-value (JSON resolver) path.
            var columnTypeName = TypeNameFormatter.Format(predicateSignature.ColumnType);
            var valueTypeName = TypeNameFormatter.Format(predicateSignature.ValueType);
            return $"{OperatorFactoryType}.Value<{columnTypeName}, {valueTypeName}>({quotedName}, {memberAccess})";
        }
        if (predicateSignature.ColumnType is INamedTypeSymbol { ConstructedFrom.SpecialType: SpecialType.System_Nullable_T } nullableColumn)
        {
            return $"{OperatorFactoryType}.UnaryOverNullable<{TypeNameFormatter.Format(nullableColumn.TypeArguments[0])}>({quotedName}, {memberAccess})";
        }
        return $"{OperatorFactoryType}.Unary<{TypeNameFormatter.Format(predicateSignature.ColumnType)}>({quotedName}, {memberAccess})";
    }

    private static string? ReadOperatorName(ISymbol member)
    {
        if (member is not IPropertySymbol && member is not IMethodSymbol) return null;
        foreach (var attributeData in member.GetAttributes())
        {
            if (attributeData.AttributeClass?.ToDisplayString() != FilterOperatorAttributeFullName) continue;
            if (attributeData.ConstructorArguments.Length > 0 && attributeData.ConstructorArguments[0].Value is string operatorName)
            {
                return operatorName;
            }
        }
        return null;
    }

    private static string BridgeClassName(string profileFullName) =>
        EmissionNames.ToIdentifier(profileFullName) + "Profile";
}
