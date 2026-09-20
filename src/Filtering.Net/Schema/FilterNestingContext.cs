namespace Filtering.Net;

/// <summary>The chain of nestings a schema is currently being built through. It lets circular filter graphs stop at each nesting's <c>MaxDepth</c> and turns an unbounded cycle into a configuration error instead of endless recursion.</summary>
public sealed class FilterNestingContext
{
    private readonly NestingStep[] _path;

    private FilterNestingContext(NestingStep[] path)
    {
        _path = path;
    }

    /// <summary>The context of a schema that is not nested in anything.</summary>
    public static FilterNestingContext Root { get; } = new([]);

    // False means the nesting reached its MaxDepth on this path and contributes nothing further.
    internal bool TryEnter(string nestingKey, int maxDepth, out FilterNestingContext nestedContext)
    {
        if (string.IsNullOrWhiteSpace(nestingKey))
            throw new FilterConfigurationException("A nesting key must be a non-empty string.");
        if (maxDepth < 0)
            throw new FilterConfigurationException($"Nesting '{nestingKey}' has MaxDepth {maxDepth}; it must be zero (unbounded) or positive.");

        var isBounded = maxDepth > 0;
        if (isBounded)
        {
            var timesEntered = 0;
            foreach (var step in _path)
            {
                if (step.NestingKey == nestingKey) timesEntered++;
            }
            if (timesEntered >= maxDepth)
            {
                nestedContext = this;
                return false;
            }
        }
        else
        {
            ThrowIfUnboundedCycle(nestingKey);
        }

        var nestedPath = new NestingStep[_path.Length + 1];
        _path.CopyTo(nestedPath, 0);
        nestedPath[_path.Length] = new NestingStep(nestingKey, isBounded);
        nestedContext = new FilterNestingContext(nestedPath);
        return true;
    }

    // An unbounded nesting may repeat only when a bounded one sits between the repeats, because bounded
    // nestings run out. A repeat with nothing bounded in between would recurse forever.
    private void ThrowIfUnboundedCycle(string nestingKey)
    {
        var previousIndex = Array.FindLastIndex(_path, step => step.NestingKey == nestingKey);
        if (previousIndex < 0) return;
        for (var stepIndex = previousIndex + 1; stepIndex < _path.Length; stepIndex++)
        {
            if (_path[stepIndex].IsBounded) return;
        }

        var chain = string.Join(" -> ", _path.Skip(previousIndex).Select(step => step.NestingKey).Concat([nestingKey]));
        throw new FilterConfigurationException(
            $"Nested filters form a cycle with no depth limit: {chain}. Set MaxDepth on at least one [MapNested] in the cycle.");
    }

    private readonly struct NestingStep(string nestingKey, bool isBounded)
    {
        public string NestingKey { get; } = nestingKey;

        public bool IsBounded { get; } = isBounded;
    }
}
