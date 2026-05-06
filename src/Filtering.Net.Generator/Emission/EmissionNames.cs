using System.Text;

namespace Filtering.Net.Generator;

// Centralises naming conversions so the same property can't drift across switch cases, leaf
// methods, and validators (e.g. "Department.Name" → "Department_Name", "in" → "In").
internal static class EmissionNames
{
    public static string PropertyIdentifier(string propertyName)
    {
        var builder = new StringBuilder(propertyName.Length);
        foreach (var character in propertyName)
        {
            builder.Append(character == '.' ? '_' : character);
        }
        return builder.ToString();
    }

    public static string PropertyAccessor(string entityVariable, string propertyName)
        => entityVariable + "." + propertyName;

    public static string UpperFieldKey(string field) => field.ToUpperInvariant();

    // Capitalises the first letter so "startsWith" becomes "StartsWith" in method names.
    public static string OperatorIdentifier(string operatorName)
    {
        if (string.IsNullOrEmpty(operatorName)) return operatorName;
        var first = operatorName[0];
        if (char.IsUpper(first)) return operatorName;
        return char.ToUpperInvariant(first) + operatorName.Substring(1);
    }

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
