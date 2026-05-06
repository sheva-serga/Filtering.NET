using System.Text;

namespace Filtering.Net.Generator;

internal static class SourceEmitter
{
    public static string EmitForClass(FilterClassModel model) =>
        ScribanRuntime.Render("FilterClass", BuildView(model));

    internal static FilterClassView BuildView(FilterClassModel model)
    {
        var entityFullName = "global::" + model.FullEntityTypeName;
        var hasNamespace = !string.IsNullOrEmpty(model.Namespace);
        var indent = hasNamespace ? "        " : "    ";
        var perPropertyIndent = hasNamespace ? "    " : string.Empty;
        var configurationMethodNames = model.Properties.Select(p => p.ConfigurationMethodName).ToList();

        var validateNodeBody = Indent(ValidateNodeEmitter.Emit(model), indent);
        var validateSortBody = Indent(ValidateSortEmitter.Emit(model), indent);
        var validatePageBody = Indent(ValidatePageEmitter.Emit(model), indent);
        var applyFilterBody = Indent(ApplyFilterEmitter.Emit(model), indent);
        var applySortingBody = Indent(ApplySortingEmitter.Emit(model), indent);

        var perPropertyBodies = BuildPerPropertyBodies(model, perPropertyIndent);

        return new FilterClassView(
            Namespace: model.Namespace ?? string.Empty,
            HasNamespace: hasNamespace,
            ClassName: model.ClassName,
            EntityFullName: entityFullName,
            DefaultPageSize: model.DefaultPageSize,
            MaxPageSize: model.MaxPageSize,
            ThreadsSerializerOptions: model.HasAnyTypedValueProperty,
            ConfigurationMethodNames: configurationMethodNames,
            ValidateNodeBody: validateNodeBody,
            ValidateSortBody: validateSortBody,
            ValidatePageBody: validatePageBody,
            ApplyFilterBody: applyFilterBody,
            ApplySortingBody: applySortingBody,
            PerPropertyClassBodies: perPropertyBodies);
    }

    private static List<string> BuildPerPropertyBodies(FilterClassModel model, string indent)
    {
        var overridesByName = new Dictionary<string, PropertyOverrideModel>(StringComparer.Ordinal);
        foreach (var overrideModel in model.Overrides)
        {
            overridesByName[overrideModel.PropertyName] = overrideModel;
        }

        var bodies = new List<string>();
        foreach (var property in model.Properties)
        {
            string raw;
            if (overridesByName.TryGetValue(property.PropertyName, out var overrideForMappedProperty))
            {
                raw = PerPropertyClassEmitter.EmitOverride(model, overrideForMappedProperty);
            }
            else
            {
                raw = PerPropertyClassEmitter.Emit(model, property);
            }
            bodies.Add(Indent(raw, indent));
        }
        foreach (var overrideModel in model.Overrides)
        {
            if (model.Properties.Any(property => property.PropertyName == overrideModel.PropertyName)) continue;
            bodies.Add(Indent(PerPropertyClassEmitter.EmitOverride(model, overrideModel), indent));
        }
        return bodies;
    }

    private static string Indent(string block, string indent)
    {
        if (string.IsNullOrEmpty(block)) return block;
        var builder = new StringBuilder(block.Length + 64);
        var lines = block.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            if (line.Length > 0)
            {
                builder.Append(indent);
                builder.Append(line);
            }
            if (i < lines.Length - 1) builder.Append('\n');
        }
        return builder.ToString();
    }
}
