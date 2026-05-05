using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator;

/// <summary>
/// Cacheable description of a diagnostic produced during model extraction. Uses primitive/value
/// data so that it does not break equality of the surrounding model when flowing through the
/// incremental pipeline. The corresponding <see cref="Diagnostic"/> is materialized only at
/// emission time via <see cref="ToDiagnostic"/>.
/// </summary>
internal sealed record DiagnosticInfo(
    string Id,
    string Title,
    string MessageFormat,
    DiagnosticSeverity Severity,
    LocationInfo? Location,
    EquatableList<string> MessageArgs)
{
    public Diagnostic ToDiagnostic()
    {
        var descriptor = new DiagnosticDescriptor(
            id: Id,
            title: Title,
            messageFormat: MessageFormat,
            category: "Filtering.Net",
            defaultSeverity: Severity,
            isEnabledByDefault: true);
        var location = Location?.ToLocation() ?? Microsoft.CodeAnalysis.Location.None;
        var args = new object[MessageArgs.Count];
        for (var argIndex = 0; argIndex < MessageArgs.Count; argIndex++)
        {
            args[argIndex] = MessageArgs[argIndex];
        }
        return Diagnostic.Create(descriptor, location, args);
    }

    public static DiagnosticInfo From(DiagnosticDescriptor descriptor, Location? location, params string[] messageArgs)
    {
        return new DiagnosticInfo(
            Id: descriptor.Id,
            Title: descriptor.Title.ToString(),
            MessageFormat: descriptor.MessageFormat.ToString(),
            Severity: descriptor.DefaultSeverity,
            Location: LocationInfo.FromLocation(location),
            MessageArgs: new EquatableList<string>(messageArgs));
    }
}
