// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;

namespace Aspire.Hosting.Yaml;

/// <summary>
/// Provides functionality to construct YAML strings using a programmatic approach.
/// </summary>
public sealed class YamlWriter
{
    private readonly StringBuilder _builder = new();

    /// <summary>
    /// Writes a YAML-compatible scalar value to the underlying string builder.
    /// </summary>
    /// <param name="value">An object representing the value to write. The value is converted to a YAML scalar format based on its type, such as numbers, booleans, strings, or null.</param>
    public void WriteValue(object value)
    {
        // Manually handle quoting logic.
        var scalar = ConvertToYamlScalar(value);
        _builder.AppendLine(scalar);
    }

    /// <summary>
    /// Writes a YAML-compatible property name to the underlying string builder.
    /// </summary>
    /// <param name="name">A string representing the property name to write. The property name is output followed by a colon and a space, conforming to YAML syntax.</param>
    public void WritePropertyName(string name)
    {
        // Output the property name followed by colon + space.
        _builder.Append(name + ": ");
    }

    /// <summary>
    /// Begins the definition of a YAML object by writing an empty line, which separates objects and adheres to YAML formatting.
    /// </summary>
    public void WriteStartObject()
    {
        _builder.AppendLine();
    }

    /// <summary>
    /// Ends the definition of a YAML object by writing an empty line
    /// to the underlying string builder. This ensures proper separation
    /// and adherence to YAML formatting conventions.
    /// </summary>
    public void WriteEndObject()
    {
        _builder.AppendLine();
    }

    /// <summary>
    /// Begins the definition of a YAML-compatible array by writing an opening square bracket
    /// to the underlying string builder. This indicates the start of a collection of items
    /// in YAML syntax.
    /// </summary>
    public void WriteStartArray()
    {
        _builder.AppendLine("[");
    }

    /// <summary>
    /// Ends the definition of a YAML-compatible array by writing a closing square bracket
    /// to the underlying string builder. This signifies the end of a collection of items
    /// in YAML syntax.
    /// </summary>
    public void WriteEndArray()
    {
        _builder.AppendLine("]");
    }

    /// <summary>
    /// Retrieves the current YAML string that has been constructed using the writer.
    /// </summary>
    /// <returns>A string representing the YAML content created by the writer.</returns>
    public string Compile() => _builder.ToString();

    private static string ConvertToYamlScalar(object value) =>
        value switch
        {
            null => "null",
            // Keep booleans as native
            bool b => b ? "true" : "false",
            int i => i.ToString(CultureInfo.InvariantCulture),
            long l => l.ToString(CultureInfo.InvariantCulture),
            float f => f.ToString(CultureInfo.InvariantCulture),
            double d => d.ToString(CultureInfo.InvariantCulture),
            string s => QuoteString(s),
            _ => QuoteString(value.ToString() ?? "")
        };

    private static string QuoteString(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            // represent empty string as ''
            return "''";
        }

        // If it has newlines, let's do a naive multiline approach:
        if (s.Contains('\n'))
        {
            var lines = s.Split('\n');
            var sb = new StringBuilder();
            sb.AppendLine("|");
            foreach (var line in lines)
            {
                sb.AppendLine("  " + line);
            }
            return sb.ToString();
        }

        // If the string is purely safe unquoted (like alphanumerics + some punctuation),
        // we can omit quotes. But let's be safe & detect tricky chars quickly:
        if (IsSafeUnquoted(s))
        {
            // If it won't parse as a boolean or numeric accidentally, we can leave it unquoted.
            // But let's be safer and check if it's YAML boolean or numeric
            if (YamlMightInterpretAsBooleanOrNumber(s))
            {
                // then we definitely quote
                return SingleQuote(s);
            }
            return s; // no quotes
        }

        // Otherwise decide between single vs double quotes:
        if (s.Contains('\''))
        {
            // if it has single quotes, we use double quotes and escape any double quotes
            // e.g. Hello "there" => "Hello \"there\""
            return DoubleQuoteEscape(s);
        }

        // use single quotes and escape single quotes if needed
        return SingleQuote(s);
    }

    private static bool IsSafeUnquoted(string s) =>
        s.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-');

    private static bool YamlMightInterpretAsBooleanOrNumber(string s)
    {
        var lower = s.ToLowerInvariant();
        if (lower is "true" or "false" or "null")
        {
            return true;
        }

        // if it's a valid integer
        if (int.TryParse(s, out _))
        {
            return true;
        }

        // if it's a valid float/double
        return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
    }

    private static string SingleQuote(string s)
    {
        // In YAML, single quotes are escaped by doubling them...
        var escaped = s.Replace("'", "''");
        return $"'{escaped}'";
    }

    private static string DoubleQuoteEscape(string s)
    {
        var escaped = s.Replace("\"", "\\\"");
        return $"\"{escaped}\"";
    }
}
