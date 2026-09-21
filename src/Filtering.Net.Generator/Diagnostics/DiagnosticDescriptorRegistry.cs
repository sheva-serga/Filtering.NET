using System.Reflection;

using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator;

// Every registered descriptor, keyed by id. DiagnosticInfo carries only the id through the
// incremental pipeline and looks the descriptor up again here, so a reported diagnostic keeps the
// help link and description the catalogue page promises instead of a rebuilt stand-in.
internal static class DiagnosticDescriptorRegistry
{
    private static readonly Dictionary<string, DiagnosticDescriptor> DescriptorsById = BuildDescriptorsById();

    public static DiagnosticDescriptor ById(string diagnosticId) =>
        DescriptorsById.TryGetValue(diagnosticId, out var descriptor)
            ? descriptor
            : throw new FilterEmissionException($"No DiagnosticDescriptor is registered for id '{diagnosticId}'.");

    private static Dictionary<string, DiagnosticDescriptor> BuildDescriptorsById()
    {
        var descriptorsById = new Dictionary<string, DiagnosticDescriptor>(StringComparer.Ordinal);
        foreach (var field in typeof(DiagnosticDescriptors).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is DiagnosticDescriptor descriptor)
            {
                descriptorsById[descriptor.Id] = descriptor;
            }
        }
        return descriptorsById;
    }
}
