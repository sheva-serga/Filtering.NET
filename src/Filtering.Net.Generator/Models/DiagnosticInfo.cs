using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator;

// Primitive-only diagnostic snapshot so it doesn't break incremental-pipeline equality.
// The actual Diagnostic is materialised at emission time via ToDiagnostic().
internal sealed record DiagnosticInfo(
    string Id,
    string Title,
    string MessageFormat,
    DiagnosticSeverity Severity,
    LocationInfo? Location,
    EquatableList<LocationInfo> AdditionalLocations,
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
        var primaryLocation = Location?.ToLocation() ?? Microsoft.CodeAnalysis.Location.None;
        var args = new object[MessageArgs.Count];
        for (var argIndex = 0; argIndex < MessageArgs.Count; argIndex++)
        {
            args[argIndex] = MessageArgs[argIndex];
        }

        if (AdditionalLocations.Count == 0)
        {
            return Diagnostic.Create(descriptor, primaryLocation, args);
        }

        var additionalLocations = new Location[AdditionalLocations.Count];
        for (var locationIndex = 0; locationIndex < AdditionalLocations.Count; locationIndex++)
        {
            additionalLocations[locationIndex] = AdditionalLocations[locationIndex].ToLocation();
        }
        return Diagnostic.Create(
            descriptor,
            primaryLocation,
            additionalLocations,
            properties: null,
            messageArgs: args);
    }

    public static DiagnosticInfo From(DiagnosticDescriptor descriptor, Location? location, params string[] messageArgs)
    {
        return new DiagnosticInfo(
            Id: descriptor.Id,
            Title: descriptor.Title.ToString(),
            MessageFormat: descriptor.MessageFormat.ToString(),
            Severity: descriptor.DefaultSeverity,
            Location: LocationInfo.FromLocation(location),
            AdditionalLocations: new EquatableList<LocationInfo>(),
            MessageArgs: new EquatableList<string>(messageArgs));
    }

    public static DiagnosticInfo From(
        DiagnosticDescriptor descriptor,
        Location? location,
        Location[] additionalLocations,
        params string[] messageArgs)
    {
        var wrappedAdditionalLocations = new List<LocationInfo>(additionalLocations.Length);
        foreach (var additionalLocation in additionalLocations)
        {
            var wrapped = LocationInfo.FromLocation(additionalLocation);
            if (wrapped is not null)
            {
                wrappedAdditionalLocations.Add(wrapped);
            }
        }
        return new DiagnosticInfo(
            Id: descriptor.Id,
            Title: descriptor.Title.ToString(),
            MessageFormat: descriptor.MessageFormat.ToString(),
            Severity: descriptor.DefaultSeverity,
            Location: LocationInfo.FromLocation(location),
            AdditionalLocations: new EquatableList<LocationInfo>(wrappedAdditionalLocations),
            MessageArgs: new EquatableList<string>(messageArgs));
    }
}
