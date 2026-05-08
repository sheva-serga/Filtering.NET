using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Filtering.Net.Generator;

// Primitive-only record so it passes through the incremental pipeline without breaking equality
// (Roslyn's Location captures non-equatable SyntaxTree references).
internal sealed record LocationInfo(string FilePath, TextSpan TextSpan, LinePositionSpan LineSpan)
{
    public Location ToLocation() => Location.Create(FilePath, TextSpan, LineSpan);

    public static LocationInfo? FromLocation(Location? location)
    {
        if (location is null) return null;
        if (location == Location.None) return null;
        // MetadataFile locations (BCL types and referenced assemblies) cannot survive a
        // Location.Create round-trip — their line span carries no usable path.
        if (location.Kind == LocationKind.MetadataFile) return null;
        if (location.SourceTree is not null)
        {
            return new LocationInfo(
                location.SourceTree.FilePath,
                location.SourceSpan,
                location.GetLineSpan().Span);
        }
        // External-file locations (produced by a prior LocationInfo.ToLocation round-trip) carry
        // FilePath/TextSpan/LineSpan but no SourceTree; preserve them so resolver-emitted diagnostics
        // keep their original positions through the additionalLocations argument.
        var lineSpan = location.GetLineSpan();
        return new LocationInfo(lineSpan.Path, location.SourceSpan, lineSpan.Span);
    }
}
