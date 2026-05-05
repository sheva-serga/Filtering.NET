namespace Filtering.Net;

/// <summary>A combinator node holding child <see cref="FilterNode"/> instances joined by <paramref name="Op"/>.</summary>
/// <param name="Op">Logical combinator: And, Or, or Not.</param>
/// <param name="Children">Child filter nodes. For <see cref="LogicalOp.Not"/>, must contain exactly one child.</param>
public sealed record FilterGroup(LogicalOp Op, IReadOnlyList<FilterNode> Children) : FilterNode;
