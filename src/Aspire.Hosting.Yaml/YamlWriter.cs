// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;

namespace Aspire.Hosting.Yaml;

/// <summary>
/// Provides functionality to write YAML formatted data as a string.
/// </summary>
/// <remarks>
/// The <see cref="YamlWriter"/> class is designed to serialize objects and arrays into a YAML-formatted string representation.
/// It keeps track of indentation levels to generate properly indented YAML output.
/// </remarks>
public sealed class YamlWriter
{
    /// <summary>
    /// StringBuilder instance used to accumulate YAML content during writing operations.
    /// Provides an efficient mechanism for appending and managing text data
    /// required for YAML serialization processes.
    /// </summary>
    private readonly StringBuilder _builder = new();

    /// <summary>
    /// Represents the current indentation level for formatting YAML output.
    /// Used to determine the number of spaces to prepend for nested elements.
    /// </summary>
    private int _indentLevel = -1;

    /// <summary>
    /// Determines the number of spaces used for each level of indentation.
    /// Used to format YAML output consistently and improve readability.
    /// </summary>
    private const int SpacesPerIndent = 2;

    /// <summary>
    /// Writes the specified property name to the YAML output buffer, followed by a colon.
    /// </summary>
    /// <param name="name">The name of the property to write.</param>
    public void WritePropertyName(string name)
    {
        WriteIndent();
        _builder.Append(name);
        _builder.Append(':');
    }

    /// <summary>
    /// Writes a value to the underlying YAML representation.
    /// Handles both single-line and multi-line values, formatting them appropriately.
    /// </summary>
    /// <param name="value">The value to be written. Can be a scalar value or an object convertible to a YAML string.</param>
    public void WriteValue(object value)
    {
        var scalar = ConvertToYamlScalar(value);

        // If multi-line (block string), each line is separate
        if (scalar.Contains('\n'))
        {
            _builder.AppendLine(); // new line after property name
            var lines = scalar.Split('\n');
            foreach (var line in lines)
            {
                WriteIndent();
                _builder.AppendLine(line);
            }
        }
        else
        {
            // Single-line scalar
            _builder.Append(' ');
            _builder.AppendLine(scalar);
        }
    }

    /// <summary>
    /// Writes the start of a YAML object by appending a newline and increasing the current level of indentation.
    /// </summary>
    public void WriteStartObject()
    {
        _builder.AppendLine();   // newline
        _indentLevel++;
    }

    /// <summary>
    /// Decreases the current level of indentation, indicating the end of an object block in the YAML structure.
    /// This method adjusts the internal state of the writer, allowing proper formatting and alignment
    /// of subsequent YAML content at the correct indentation level.
    /// </summary>
    public void WriteEndObject()
    {
        _indentLevel--;
    }

    /// <summary>
    /// Writes the start of a YAML array to the internal buffer.
    /// This includes a new line, proper indentation, and the opening square bracket "[".
    /// </summary>
    public void WriteStartArray()
    {
        _builder.AppendLine();
        WriteIndent();
        _builder.AppendLine("[");
        _indentLevel++;
    }

    /// <summary>
    /// Ends a YAML array by reducing the current indentation level
    /// and appending the closing bracket (']') to the YAML content.
    /// </summary>
    public void WriteEndArray()
    {
        _indentLevel--;
        WriteIndent();
        _builder.AppendLine("]");
    }

    /// <summary>
    /// Compiles and returns the YAML content as a formatted string.
    /// </summary>
    /// <returns>A string containing the complete YAML representation of the data written to the writer.</returns>
    public string Compile() => _builder.ToString();

    /// <summary>
    /// Writes the appropriate number of spaces for the current indentation level.
    /// Indentation is determined by the current indent level multiplied by the fixed number of spaces per indent level.
    /// Root-level (indent level less than 0) generates no spaces.
    /// </summary>
    private void WriteIndent()
    {
        // If _indentLevel < 0, we treat it as 0 => no indent for root-level
        var level = _indentLevel < 0 ? 0 : _indentLevel;
        _builder.Append(new string(' ', level * SpacesPerIndent));
    }

    /// <summary>
    /// Converts a given value to its corresponding YAML scalar representation.
    /// The conversion supports null values, booleans, numbers, and strings.
    /// Strings may be quoted or escaped appropriately based on their content.
    /// </summary>
    /// <param name="value">The object to be converted to a YAML scalar.</param>
    /// <returns>A string containing the YAML scalar representation of the provided value.</returns>
    private static string ConvertToYamlScalar(object value) =>
        value switch
        {
            null => "null",
            bool b => b ? "true" : "false",
            int i => i.ToString(CultureInfo.InvariantCulture),
            long l => l.ToString(CultureInfo.InvariantCulture),
            float f => f.ToString(CultureInfo.InvariantCulture),
            double d => d.ToString(CultureInfo.InvariantCulture),
            string s => QuoteString(s),
            _ => QuoteString(value.ToString() ?? "")
        };

    /// <summary>
    /// Converts the provided string into a YAML-compatible format with appropriate quoting, escaping,
    /// and representation for safe inclusion in a YAML document.
    /// </summary>
    /// <param name="s">The input string to process and format as a YAML scalar.</param>
    /// <returns>A YAML-formatted string that ensures proper escaping and quoting for YAML compatibility.</returns>
    private static string QuoteString(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return "''";
        }

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

        if (IsSafeUnquoted(s))
        {
            if (YamlMightInterpretAsBooleanOrNumber(s))
            {
                return SingleQuote(s);
            }
            return s;
        }

        if (s.Contains('\''))
        {
            return DoubleQuoteEscape(s);
        }
        return SingleQuote(s);
    }

    /// <summary>
    /// Determines if the given string can be safely represented without quotes in YAML.
    /// </summary>
    /// <param name="s">The string to evaluate for safe, unquoted usage in YAML.</param>
    /// <returns>
    /// <c>true</c> if the string contains only characters that are safe for unquoted YAML representation;
    /// otherwise, <c>false</c>.
    /// </returns>
    private static bool IsSafeUnquoted(string s) =>
        s.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-');

    /// <summary>
    /// Determines whether a given string might be interpreted by YAML as a boolean or a number.
    /// </summary>
    /// <param name="s">The string to evaluate.</param>
    /// <returns>
    /// Returns true if the string could be interpreted as a boolean or number in YAML; otherwise, false.
    /// </returns>
    private static bool YamlMightInterpretAsBooleanOrNumber(string s)
    {
        var lower = s.ToLowerInvariant();
        if (lower is "true" or "false" or "null")
        {
            return true;
        }
        if (int.TryParse(s, out _))
        {
            return true;
        }
        return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
    }

    /// Generates a YAML-compliant, single-quoted string from the provided input string.
    /// <param name="s">The input string to be quoted.</param>
    /// <return>A YAML-safe, single-quoted representation of the input string.</return>
    private static string SingleQuote(string s)
    {
        var escaped = s.Replace("'", "''");
        return $"'{escaped}'";
    }

    /// <summary>
    /// Escapes double quotes within a string and wraps the string in double quotes.
    /// </summary>
    /// <param name="s">The string to be escaped and wrapped in double quotes.</param>
    /// <returns>A double-quoted string where all double quotes are escaped with a backslash.</returns>
    private static string DoubleQuoteEscape(string s)
    {
        var escaped = s.Replace("\"", "\\\"");
        return $"\"{escaped}\"";
    }
}
