namespace Filtering.Net.Generator;

/// <summary>
/// Emitter for the assembly-wide <c>AddFiltering</c> DI extension. Emits one
/// <c>FilteringServiceCollectionExtensions</c> static class that registers every discovered
/// filter class as <c>AddSingleton&lt;IFilterDefinition&lt;TEntity&gt;, FilterClass&gt;()</c>.
/// </summary>
internal static class DiExtensionEmitter
{
    /// <summary>The metadata name (case-insensitive) of Microsoft.Extensions.DependencyInjection.Abstractions.</summary>
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

    /// <summary>True when the compilation references the
    /// <c>Microsoft.Extensions.DependencyInjection.Abstractions</c> package.</summary>
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
