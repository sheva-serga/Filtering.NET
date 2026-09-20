namespace Filtering.Net.Generator;

internal sealed record ProfileBridgeFileView(
    string GeneratedNamespace,
    IReadOnlyList<ProfileBridgeView> Bridges);

// Initializer is the complete right-hand side of the Instance field, already formatted in C#.
internal sealed record ProfileBridgeView(
    string ProfileFullName,
    string BridgeClassName,
    string ColumnTypeFqn,
    string Initializer);
