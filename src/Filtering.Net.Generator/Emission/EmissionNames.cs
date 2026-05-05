using System.Text;

namespace Filtering.Net.Generator;

/// <summary>
/// Helpers for converting model strings into legal C# identifiers used in generated method names
/// (e.g., "Department.Name" -> "Department_Name", "in" -> "In"). The same property may surface
/// multiple times in different naming roles (switch case, leaf method, validator); centralising
/// the logic avoids drift.
/// </summary>
internal static class EmissionNames
{
    /// <summary>Returns the property name converted into a legal identifier suffix.
    /// Replaces <c>.</c> with <c>_</c> for navigation paths so "Department.Name" becomes
    /// "Department_Name" — usable inside method names like <c>Validate{Suffix}_Leaf</c>.</summary>
    public static string PropertyIdentifier(string propertyName)
    {
        var builder = new StringBuilder(propertyName.Length);
        foreach (var character in propertyName)
        {
            builder.Append(character == '.' ? '_' : character);
        }
        return builder.ToString();
    }

    /// <summary>The accessor expression used inside generated lambdas. For
    /// "Department.Name" this becomes "entity.Department.Name".</summary>
    public static string PropertyAccessor(string entityVariable, string propertyName)
        => entityVariable + "." + propertyName;

    /// <summary>The uppercase switch key for a field name (matches <c>ToUpperInvariant()</c>).</summary>
    public static string UpperFieldKey(string field) => field.ToUpperInvariant();

    /// <summary>Camel-case-from-PascalCase conversion for use in method-suffix names emitted
    /// after an operator name (e.g., "startsWith" -> "StartsWith").</summary>
    public static string OperatorIdentifier(string operatorName)
    {
        if (string.IsNullOrEmpty(operatorName)) return operatorName;
        var first = operatorName[0];
        if (char.IsUpper(first)) return operatorName;
        return char.ToUpperInvariant(first) + operatorName.Substring(1);
    }

    /// <summary>Escapes a string for embedding inside a C# verbatim/double-quoted string.
    /// We use double-quoted strings everywhere, so we need to escape backslash and double quote.</summary>
    public static string EscapeStringLiteral(string value)
    {
        var builder = new StringBuilder(value.Length + 2);
        foreach (var character in value)
        {
            switch (character)
            {
                case '\\': builder.Append("\\\\"); break;
                case '"': builder.Append("\\\""); break;
                case '\r': builder.Append("\\r"); break;
                case '\n': builder.Append("\\n"); break;
                case '\t': builder.Append("\\t"); break;
                default: builder.Append(character); break;
            }
        }
        return builder.ToString();
    }
}
