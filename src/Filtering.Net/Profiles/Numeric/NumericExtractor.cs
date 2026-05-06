using System.Text.Json;

namespace Filtering.Net;

/// <summary>Generic JSON-to-numeric extraction shared by every per-CLR-type numeric profile.</summary>
public static class NumericExtractor
{
    /// <summary>Per-CLR-type JSON Number reader (e.g. <c>JsonElement.TryGetInt32</c>).</summary>
    public delegate bool TryGetFromJson<T>(JsonElement element, out T value);

    /// <summary>Per-CLR-type invariant-culture string parser (e.g. <c>int.TryParse</c> with invariant culture).</summary>
    public delegate bool TryParseInvariant<T>(string raw, out T value);

    /// <summary>Per-CLR-type scalar extractor signature; <see cref="TryGetArray{T}"/> walks an array using one of these.</summary>
    public delegate bool ScalarTryGet<T>(JsonElement element, out T value, out string error);

    /// <summary>Extracts <typeparamref name="T"/> from a JSON Number or invariant-culture JSON String.</summary>
    public static bool TryGetValue<T>(
        JsonElement element,
        TryGetFromJson<T> tryGetFromJson,
        TryParseInvariant<T> tryParseInvariant,
        string typeDisplayName,
        out T value,
        out string error)
        where T : struct
    {
        if (element.ValueKind == JsonValueKind.Number)
        {
            if (tryGetFromJson(element, out value))
            {
                error = string.Empty;
                return true;
            }
            value = default;
            error = $"Number out of range for {typeDisplayName}.";
            return false;
        }
        if (element.ValueKind == JsonValueKind.String)
        {
            var rawString = element.GetString();
            if (rawString is not null && tryParseInvariant(rawString, out value))
            {
                error = string.Empty;
                return true;
            }
            value = default;
            error = $"String '{rawString}' is not a valid invariant {typeDisplayName}.";
            return false;
        }
        value = default;
        error = $"Expected JSON Number or String for {typeDisplayName}, got {element.ValueKind}.";
        return false;
    }

    /// <summary>Walks a JSON Array via <paramref name="tryGetScalar"/>, short-circuiting on the first element failure.</summary>
    public static bool TryGetArray<T>(
        JsonElement element,
        ScalarTryGet<T> tryGetScalar,
        out T[] values,
        out string error)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            values = [];
            error = $"Expected JSON Array, got {element.ValueKind}.";
            return false;
        }
        var collected = new List<T>();
        var elementIndex = 0;
        foreach (var item in element.EnumerateArray())
        {
            if (!tryGetScalar(item, out var itemValue, out var itemError))
            {
                values = [];
                error = $"Array element [{elementIndex}]: {itemError}";
                return false;
            }
            collected.Add(itemValue);
            elementIndex++;
        }
        values = [.. collected];
        error = string.Empty;
        return true;
    }
}
