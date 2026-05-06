namespace Filtering.Net.Generator;

internal static class DiExtensionEmitter
{
    public const string DiAbstractionsAssemblyName = "Microsoft.Extensions.DependencyInjection.Abstractions";

    public static string Emit(IReadOnlyList<FilterClassModel> models, string consumerAssemblyName)
    {
        _ = consumerAssemblyName;
        return ScribanRuntime.Render("DiExtension", BuildView(models));
    }

    internal static DiExtensionView BuildView(IReadOnlyList<FilterClassModel> models)
    {
        var registrations = new List<DiRegistrationView>(models.Count);
        foreach (var model in models)
        {
            var entityFullName = "global::" + model.FullEntityTypeName;
            var classFullName = string.IsNullOrEmpty(model.Namespace)
                ? "global::" + model.ClassName
                : $"global::{model.Namespace}.{model.ClassName}";
            registrations.Add(new DiRegistrationView(entityFullName, classFullName, model.HasAnyTypedValueProperty));
        }
        return new DiExtensionView(registrations);
    }

    public static bool IsDiAbstractionsReferenced(Microsoft.CodeAnalysis.Compilation compilation)
    {
        foreach (var reference in compilation.ReferencedAssemblyNames)
        {
            if (string.Equals(reference.Name, DiAbstractionsAssemblyName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
