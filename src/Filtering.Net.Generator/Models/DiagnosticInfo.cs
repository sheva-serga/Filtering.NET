using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator;

// Primitive-only diagnostic snapshot so it doesn't break incremental-pipeline equality: the
// registered descriptor is looked up again by id at emission time, which keeps the help link and
// the description the catalogue promises without putting a DiagnosticDescriptor in a cached model.
internal sealed record DiagnosticInfo(
    string Id,
    LocationInfo? Location,
    EquatableList<LocationInfo> AdditionalLocations,
    EquatableList<string> MessageArgs)
{
    public Diagnostic ToDiagnostic()
    {
        var descriptor = DiagnosticDescriptorRegistry.ById(Id);
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
            Location: LocationInfo.FromLocation(location),
            AdditionalLocations: new EquatableList<LocationInfo>(),
            MessageArgs: new EquatableList<string>(messageArgs));
    }

    public static DiagnosticInfo From(DiagnosticDescriptor descriptor, LocationInfo? location, params string[] messageArgs)
    {
        return new DiagnosticInfo(
            Id: descriptor.Id,
            Location: location,
            AdditionalLocations: new EquatableList<LocationInfo>(),
            MessageArgs: new EquatableList<string>(messageArgs));
    }

    public static DiagnosticInfo From(
        DiagnosticDescriptor descriptor,
        LocationInfo? location,
        IReadOnlyList<LocationInfo?> additionalLocations,
        params string[] messageArgs)
    {
        var presentAdditionalLocations = new List<LocationInfo>(additionalLocations.Count);
        foreach (var additionalLocation in additionalLocations)
        {
            if (additionalLocation is not null) presentAdditionalLocations.Add(additionalLocation);
        }
        return new DiagnosticInfo(
            Id: descriptor.Id,
            Location: location,
            AdditionalLocations: new EquatableList<LocationInfo>(presentAdditionalLocations),
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
            Location: LocationInfo.FromLocation(location),
            AdditionalLocations: new EquatableList<LocationInfo>(wrappedAdditionalLocations),
            MessageArgs: new EquatableList<string>(messageArgs));
    }
}
