using System.Text;

namespace Filtering.Net.Generator;

internal static class EmissionNames
{
    // Folds a fully-qualified type name into a single C# identifier, so a generated class name can
    // stay unique across namespaces and nesting without any compilation-order dependence.
    public static string ToIdentifier(string fullyQualifiedName)
    {
        var identifier = new StringBuilder(fullyQualifiedName.Length);
        foreach (var character in fullyQualifiedName)
        {
            identifier.Append(char.IsLetterOrDigit(character) ? character : '_');
        }
        return identifier.ToString();
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

    // A generic profile's display name carries angle brackets, which would make the XML doc comment
    // that quotes it malformed (CS1570) in a consumer project that generates a documentation file.
    public static string EscapeXmlText(string value)
    {
        var builder = new StringBuilder(value.Length + 8);
        foreach (var character in value)
        {
            switch (character)
            {
                case '&': builder.Append("&amp;"); break;
                case '<': builder.Append("&lt;"); break;
                case '>': builder.Append("&gt;"); break;
                default: builder.Append(character); break;
            }
        }
        return builder.ToString();
    }
}
