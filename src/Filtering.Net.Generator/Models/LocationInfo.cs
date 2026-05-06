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
        if (location.SourceTree is null) return null;
        return new LocationInfo(
            location.SourceTree.FilePath,
            location.SourceSpan,
            location.GetLineSpan().Span);
    }
}
