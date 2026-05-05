using System.Text.Json;

namespace Filtering.Net;

/// <summary>A leaf node: a single field/operator/value triple.</summary>
/// <param name="Field">The configured field name (alias) being filtered.</param>
/// <param name="Operator">The operator to apply (e.g., "eq", "contains").</param>
/// <param name="Value">The raw JSON value to compare against.</param>
public sealed record FilterLeaf(string Field, string Operator, JsonElement Value) : FilterNode;
