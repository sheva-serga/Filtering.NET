namespace Filtering.Net.Generator;

internal sealed record ProfileBridgeFileView(
    string GeneratedNamespace,
    IReadOnlyList<ProfileBridgeView> Bridges);

// Initializer is the complete right-hand side of the Instance field, already formatted in C#.
// ProfileDisplayNameXml is the same profile name with XML text escaped, for the doc comment.
internal sealed record ProfileBridgeView(
    string ProfileFullName,
    string ProfileDisplayNameXml,
    string BridgeClassName,
    string ColumnTypeFqn,
    string Initializer);
