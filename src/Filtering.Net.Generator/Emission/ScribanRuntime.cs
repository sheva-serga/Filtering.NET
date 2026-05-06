using System.Collections.Concurrent;

using Scriban;
using Scriban.Runtime;

namespace Filtering.Net.Generator;

// Templates are cached for the lifetime of the assembly load.
internal static class ScribanRuntime
{
    private const string ResourceNamespace = "Filtering.Net.Generator.Emission.Templates.";
    private static readonly ConcurrentDictionary<string, Template> Cache = new();

    // PascalCase C# property names appear in templates as snake_case via StandardMemberRenamer.
    public static string Render(string templateName, object view)
    {
        var template = Cache.GetOrAdd(templateName, LoadAndParse);
        var scriptObject = new ScriptObject();
        scriptObject.Import(view);
        scriptObject.Import("to_operator_id", new Func<string, string>(EmissionNames.OperatorIdentifier));
        var context = new TemplateContext();
        context.PushGlobal(scriptObject);
        return template.Render(context);
    }

    private static Template LoadAndParse(string templateName)
    {
        var resourceName = ResourceNamespace + templateName + ".scriban";
        var assembly = typeof(ScribanRuntime).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            throw new FilterEmissionException(
                $"Embedded Scriban template '{resourceName}' not found (asked for '{templateName}').");
        }
        using var reader = new StreamReader(stream);
        var source = reader.ReadToEnd();
        var template = Template.Parse(source, sourceFilePath: resourceName);
        if (template.HasErrors)
        {
            throw new FilterEmissionException(
                $"Scriban template '{templateName}' failed to parse: {template.Messages}");
        }
        return template;
    }
}
