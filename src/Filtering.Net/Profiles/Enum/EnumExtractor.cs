using System.Text.Json;

namespace Filtering.Net;

/// <summary>Generic JSON-to-enum extraction shared by the per-enum profile classes the source generator emits.</summary>
public static class EnumExtractor
{
    /// <summary>Extracts <typeparamref name="TEnum"/> from a case-insensitive JSON String or a JSON Number (underlying integral).</summary>
    /// <typeparam name="TEnum">The target enum type.</typeparam>
    /// <param name="element">The JSON value to read.</param>
    /// <param name="value">The extracted enum value, or <c>default</c> on failure.</param>
    /// <param name="error">A human-readable explanation of the failure.</param>
    public static bool TryGetValue<TEnum>(JsonElement element, out TEnum value, out string error)
        where TEnum : struct, Enum
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            var rawString = element.GetString();
            if (rawString is not null && Enum.TryParse(rawString, ignoreCase: true, out value))
            {
                error = string.Empty;
                return true;
            }
            value = default;
            error = $"String '{rawString}' is not a valid member of enum {typeof(TEnum).Name}.";
            return false;
        }
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var integralValue))
        {
            try
            {
                value = (TEnum)Enum.ToObject(typeof(TEnum), integralValue);
                error = string.Empty;
                return true;
            }
            catch (Exception conversionFailure)
            {
                value = default;
                error = conversionFailure.Message;
                return false;
            }
        }
        value = default;
        error = $"Expected JSON String or Number for enum {typeof(TEnum).Name}, got {element.ValueKind}.";
        return false;
    }

    /// <summary>Extracts a <typeparamref name="TEnum"/>[] from a JSON Array; each element parsed via <see cref="TryGetValue{TEnum}"/>.</summary>
    public static bool TryGetArray<TEnum>(JsonElement element, out TEnum[] values, out string error)
        where TEnum : struct, Enum
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            values = [];
            error = $"Expected JSON Array for enum array, got {element.ValueKind}.";
            return false;
        }
        var collected = new List<TEnum>();
        var elementIndex = 0;
        foreach (var item in element.EnumerateArray())
        {
            if (!TryGetValue<TEnum>(item, out var itemValue, out var itemError))
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
