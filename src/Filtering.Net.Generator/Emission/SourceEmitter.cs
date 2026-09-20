using System.Text;

namespace Filtering.Net.Generator;

// Emits the generated half of a [GenerateFilter<TEntity>] class: the FilterDefinition<TEntity> base,
// constructors, marker-method bodies, and CreateSchema. All filtering logic lives in the runtime.
internal static class SourceEmitter
{
    private const string EntityParameterName = "entity";
    private const string PropertyFactoryType = "global::Filtering.Net.FilterProperty";
    // Scriban re-indents multi-line values to the column of the tag, so this is relative to the entry.
    private const string ContinuationIndent = "    ";

    public static string EmitForClass(FilterClassModel model) =>
        ScribanRuntime.Render("FilterClass", BuildView(model));

    internal static FilterClassView BuildView(FilterClassModel model)
    {
        var entityFullName = "global::" + model.FullEntityTypeName;

        // Only the host's own partial methods get implementation parts. Properties spliced in by the
        // nested resolver carry the inner filter's method names, which are not declared on this host.
        var configurationMethodNames = model.Properties
            .Where(property => property.SourceFilterClassFqn is null)
            .Select(property => property.ConfigurationMethodName)
            .Concat(model.NestedMappings.Select(nestedMapping => nestedMapping.HostMethodName))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return new FilterClassView(
            Namespace: model.Namespace ?? string.Empty,
            HasNamespace: !string.IsNullOrEmpty(model.Namespace),
            ClassName: model.ClassName,
            EntityFullName: entityFullName,
            DefaultPageSize: model.DefaultPageSize,
            MaxPageSize: model.MaxPageSize,
            MaxNestingDepth: model.MaxNestingDepth,
            MaxLeafConditions: model.MaxLeafConditions,
            ThreadsSerializerOptions: model.HasAnyTypedValueProperty,
            ConfigurationMethodNames: configurationMethodNames,
            SchemaEntries: BuildSchemaEntries(model, entityFullName));
    }

    private static List<string> BuildSchemaEntries(FilterClassModel model, string entityFullName)
    {
        var schemaEntries = new List<string>();
        var mappedPropertyNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var property in model.Properties)
        {
            // Spliced properties arrive at runtime through the nested filter's own schema.
            if (property.SourceFilterClassFqn is not null) continue;
            mappedPropertyNames.Add(property.PropertyName);
            schemaEntries.Add(BuildMapEntry(model, property, entityFullName));
        }

        foreach (var overrideModel in model.Overrides)
        {
            // A property carried by both [Map] and [PropertyMap] is already an FN0002 error.
            if (overrideModel.BuilderTypeFqn is null || mappedPropertyNames.Contains(overrideModel.PropertyName)) continue;
            schemaEntries.Add(
                $".Add({PropertyFactoryType}.MapRule({Literal(overrideModel.PropertyName)}, {overrideModel.MethodName}(new {overrideModel.BuilderTypeFqn}())).Build())");
        }

        foreach (var nestedMapping in model.NestedMappings)
        {
            if (nestedMapping.ResolvedTargetClassFqn is null) continue;
            schemaEntries.Add(BuildNestedEntry(nestedMapping, entityFullName));
        }
        return schemaEntries;
    }

    private static string BuildMapEntry(FilterClassModel model, PropertyMappingModel property, string entityFullName)
    {
        var factoryMethod = property.IsNullableValueType ? "MapNullable" : "Map";
        var accessor = $"({entityFullName} {EntityParameterName}) => {EntityParameterName}.{property.PropertyName}";
        var entry = new StringBuilder()
            .Append($".Add({PropertyFactoryType}.{factoryMethod}({Literal(property.PropertyName)}, {accessor}, {ProfileBridgeBuilder.ProfileReference(property.ProfileFullName)})");

        if (!string.IsNullOrEmpty(property.Alias))
        {
            AppendOption(entry, $".Alias({Literal(property.Alias!)})");
        }
        if (property.HasOperatorRestriction)
        {
            AppendOption(entry, $".Only({string.Join(", ", property.AllowedOperators.Select(Literal))})");
        }
        if (FindInterceptorCall(model, property.PropertyName) is { } interceptorCall)
        {
            AppendOption(entry, interceptorCall);
        }
        if (property.Sortable)
        {
            AppendOption(entry, property.DefaultSortDirection == "Desc" ? ".Sortable(global::Filtering.Net.SortDir.Desc)" : ".Sortable()");
        }
        AppendOption(entry, ".Build())");
        return entry.ToString();
    }

    private static string? FindInterceptorCall(FilterClassModel model, string propertyName)
    {
        foreach (var interceptor in model.Interceptors)
        {
            if (interceptor.PropertyName != propertyName || interceptor.ValueClrType is null) continue;
            if (interceptor.Raw) return $".InterceptRaw({interceptor.MethodName})";
            return interceptor.ValueClrType.EndsWith("[]", StringComparison.Ordinal)
                ? $".InterceptArray({interceptor.MethodName})"
                : $".Intercept({interceptor.MethodName})";
        }
        return null;
    }

    private static string BuildNestedEntry(NestedMappingModel nestedMapping, string entityFullName)
    {
        var navigation = $"{EntityParameterName} => {EntityParameterName}.{nestedMapping.NavigationPropertyName}";
        var disableSorting = nestedMapping.DisableSorting ? "true" : "false";
        return new StringBuilder()
            .Append($".AddRange(global::{nestedMapping.ResolvedTargetClassFqn}.CreateSchema(serializerOptions)")
            .Append('\n').Append(ContinuationIndent)
            .Append($".LiftInto<{entityFullName}>({navigation}, {Literal(nestedMapping.Prefix)}, only: {PathArray(nestedMapping.Only)}, except: {PathArray(nestedMapping.Except)}, disableSorting: {disableSorting}))")
            .ToString();
    }

    private static void AppendOption(StringBuilder entry, string option) =>
        entry.Append('\n').Append(ContinuationIndent).Append(option);

    private static string PathArray(EquatableList<string> paths) =>
        paths.Count == 0 ? "null" : $"new string[] {{ {string.Join(", ", paths.Select(Literal))} }}";

    private static string Literal(string value) => "\"" + EmissionNames.EscapeStringLiteral(value) + "\"";
}
