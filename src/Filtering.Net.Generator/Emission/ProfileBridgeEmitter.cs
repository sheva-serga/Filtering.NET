using System.Text;

namespace Filtering.Net.Generator;

// Emits one runtime FilterProfile<T> instance per user-declared profile referenced by the assembly's filters.
internal static class ProfileBridgeEmitter
{
    public const string HintName = "FilteringProfiles.g.cs";

    // Scriban re-indents multi-line values to the column of the tag, so this is relative to the initializer.
    private const string InitializerIndent = "    ";

    public static string Emit(IReadOnlyList<ProfileBridgeModel> bridges) =>
        ScribanRuntime.Render("ProfileBridge", BuildView(bridges));

    // The same profile reaches this point once per property that uses it; first occurrence wins.
    public static List<ProfileBridgeModel> CollectDistinct(IEnumerable<FilterClassModel> models)
    {
        var distinctBridges = new List<ProfileBridgeModel>();
        var seenProfiles = new HashSet<string>(StringComparer.Ordinal);
        foreach (var model in models)
        {
            foreach (var property in model.Properties)
            {
                foreach (var bridge in property.ProfileBridges)
                {
                    if (seenProfiles.Add(bridge.ProfileFullName)) distinctBridges.Add(bridge);
                }
            }
        }
        return distinctBridges;
    }

    internal static ProfileBridgeFileView BuildView(IReadOnlyList<ProfileBridgeModel> bridges)
    {
        var bridgeViews = new List<ProfileBridgeView>(bridges.Count);
        foreach (var bridge in bridges)
        {
            bridgeViews.Add(new ProfileBridgeView(
                ProfileFullName: bridge.ProfileFullName,
                BridgeClassName: bridge.BridgeClassName,
                ColumnTypeFqn: bridge.ColumnTypeFqn,
                Initializer: BuildInitializer(bridge)));
        }
        return new ProfileBridgeFileView(ProfileBridgeBuilder.GeneratedNamespace, bridgeViews);
    }

    private static string BuildInitializer(ProfileBridgeModel bridge)
    {
        var profileNameLiteral = "\"" + EmissionNames.EscapeStringLiteral(bridge.ProfileName) + "\"";
        var initializer = new StringBuilder(bridge.BaseProfileReference is null
            ? $"global::Filtering.Net.FilterProfile<{bridge.ColumnTypeFqn}>.Create({profileNameLiteral}"
            : $"{bridge.BaseProfileReference}.Extend({profileNameLiteral}");
        foreach (var bridgeOperator in bridge.Operators)
        {
            initializer.Append(",\n").Append(InitializerIndent).Append(bridgeOperator.OperatorFactoryCall);
        }
        return initializer.Append(");").ToString();
    }
}
