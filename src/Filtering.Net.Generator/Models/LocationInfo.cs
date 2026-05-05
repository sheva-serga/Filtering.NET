using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Filtering.Net.Generator;

/// <summary>
/// Cacheable location info. Holds only primitive/value data so it can pass through the
/// incremental pipeline without breaking equality (the Roslyn <see cref="Location"/> type
/// captures non-equatable references like <see cref="SyntaxTree"/>).
/// </summary>
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
