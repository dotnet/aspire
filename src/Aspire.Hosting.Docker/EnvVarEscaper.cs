// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Aspire.Hosting.Docker;

/// <summary>
/// Escapes environment variables in command line args that will be expressed in docker compose files.
/// 
/// Syntax patterns handled:
/// - Simple variables: $FOO, $BAR
/// - Complex variables: ${FOO}, ${BAR}
/// - Default values: ${FOO:-default}
/// - Nested variables: ${FOO${BAR}}
/// 
/// All unescaped variables are escaped by doubling the $ character.
/// </summary>
internal static class EnvVarEscaper
{
    /// <summary>
    /// Safety limits to prevent DoS attacks and excessive resource usage
    /// </summary>
    private const int MaxInputLength = 1_000_000;
    private const int MaxContentLength = 100_000;
    private const int MaxRecursionDepth = 50;

    public static void EscapeUnescapedEnvVars(ReadOnlySpan<char> input, StringBuilder result)
    {
        if (input.IsEmpty)
        {
            return;
        }

        if (input.Length > MaxInputLength)
        {
            throw new ArgumentException("Input string exceeds maximum allowed length", nameof(input));
        }

        EscapeUnescapedEnvVarsInternal(input, result, 0);
    }

    private static void EscapeUnescapedEnvVarsInternal(ReadOnlySpan<char> input, StringBuilder result, int depth)
    {
        if (depth > MaxRecursionDepth)
        {
            throw new InvalidOperationException("Maximum recursion depth exceeded while processing environment variables");
        }

        if (input.IsEmpty)
        {
            return;
        }

        // Find the first '$' that isn't preceded by another '$'
        var firstUnescaped = FindFirstUnescapedDollar(input);
        if (firstUnescaped == -1)
        {
            // No unescaped '$' found, append original string
            result.Append(input);
            return;
        }

        // Append the part of the string before the first unescaped '$'
        result.Append(input[..firstUnescaped]);

        // Process the rest of the string starting from the first unescaped '$'
        for (var i = firstUnescaped; i < input.Length;)
        {
            if (input[i] == '$' && (i == 0 || input[i - 1] != '$'))
            {
                i += ProcessVariable(input[i..], result, depth + 1);
            }
            else
            {
                result.Append(input[i]);
                i++;
            }
        }
    }

    // Finds the index of the first '$' that is not preceded by another '$'.
    private static int FindFirstUnescapedDollar(ReadOnlySpan<char> input)
    {
        for (var i = 0; i < input.Length; i++)
        {
            if (input[i] == '$' && (i == 0 || input[i - 1] != '$'))
            {
                return i;
            }
        }
        return -1;
    }

    // Processes a potential variable starting at startIndex.
    // Appends the processed/escaped string to 'result'.
    // Returns the number of characters consumed from the original string 's'.
    private static int ProcessVariable(ReadOnlySpan<char> input, StringBuilder result, int depth)
    {
        // Check if '$' is the last character
        if (input.Length == 1)
        {
            result.Append('$');
            return 1;
        }

        // Handle braced ${...} or simple $VAR syntax
        return input[1] == '{' ? ProcessBracedVariable(input, result, depth) : ProcessSimpleVariable(input, result, depth);
    }

    private static int ProcessSimpleVariable(ReadOnlySpan<char> input, StringBuilder result, int depth)
    {
        if (depth > MaxRecursionDepth)
        {
            throw new InvalidOperationException("Maximum recursion depth exceeded while processing environment variables");
        }

        // Skip the leading '$' and process the remainder
        var remaining = input[1..];
        var varLength = 0;

        // Find the end of the potential variable name (letters, digits, underscore, or hyphen)
        while (varLength < remaining.Length && (char.IsLetterOrDigit(remaining[varLength]) || remaining[varLength] == '_' || remaining[varLength] == '-'))
        {
            varLength++;
        }

        // Invalid if empty or doesn't start with letter/underscore
        if (varLength == 0 || !IsLetterOrUnderscore(remaining[0]))
        {
            result.Append('$');
            return 1;
        }

        var candidate = remaining[..varLength];
        
        // Check for invalid characters (only letters, digits, underscore allowed)
        foreach (var c in candidate)
        {
            if (!char.IsLetterOrDigit(c) && c != '_')
            {
                result.Append('$').Append(candidate);
                return varLength + 1;
            }
        }

        // Valid variable found - escape with double dollar
        result.Append("$$").Append(candidate);
        return varLength + 1;
    }

    /// <summary>
    /// Handles ${...} expressions according to Docker environment variable syntax.
    /// Supports nested variables, default values, and escaped sequences.
    /// </summary>
    private static int ProcessBracedVariable(ReadOnlySpan<char> input, StringBuilder result, int depth)
    {
        if (depth > MaxRecursionDepth)
        {
            throw new InvalidOperationException("Maximum recursion depth exceeded while processing environment variables");
        }

        // Find the matching closing brace after ${, accounting for nested braces
        var remaining = input[2..];
        var braceDepth = 1;
        var closeIndex = -1;
        
        for (var i = 0; i < remaining.Length; i++)
        {
            // Only increment depth for unescaped ${
            if (remaining[i] == '$' && i + 1 < remaining.Length && remaining[i + 1] == '{')
            {
                if (i == 0 || remaining[i - 1] != '$')
                {
                    braceDepth++;
                    i++; // Skip the '{' we just processed
                }
            }
            else if (remaining[i] == '}')
            {
                braceDepth--;
                if (braceDepth == 0)
                {
                    closeIndex = i;
                    break;
                }
            }
        }

        // If no matching brace is found, treat the whole thing as literal text
        if (closeIndex == -1)
        {
            result.Append(input);
            return input.Length;
        }

        // Extract and process the content within braces
        var content = remaining[..closeIndex];
        if (content.IsEmpty)
        {
            result.Append(input[..(closeIndex + 3)]);
            return closeIndex + 3;
        }

        // Prevent excessive allocations
        if (content.Length > MaxContentLength)
        {
            throw new ArgumentException("Variable content exceeds maximum allowed length");
        }

        // Strip whitespace for analysis while preserving original
        var stripped = new char[Math.Min(content.Length, MaxContentLength)];
        var strippedLength = 0;
        foreach (var c in content)
        {
            if (!char.IsWhiteSpace(c))
            {
                if (strippedLength >= stripped.Length)
                {
                    break;
                }
                stripped[strippedLength++] = c;
            }
        }
        var strippedSpan = stripped.AsSpan(0, strippedLength);

        if (strippedSpan.IsEmpty)
        {
            result.Append(input[..(closeIndex + 3)]);
            return closeIndex + 3;
        }

        // Check for default value syntax ${VAR:-DEFAULT}
        var sepIdx = FindDefaultSeparator(strippedSpan);
        if (sepIdx >= 0)
        {
            ProcessWithDefault(strippedSpan, sepIdx, result, depth);
            return closeIndex + 3;
        }

        // Handle simple variable case ${VAR}
        if (IsValidSimpleVariable(strippedSpan))
        {
            result.Append("$$").Append('{').Append(strippedSpan).Append('}');
            return closeIndex + 3;
        }

        // Handle nested variables ${FOO${BAR}}
        if (FindFirstUnescapedDollar(strippedSpan) != -1)
        {
            result.Append("$$").Append('{');
            EscapeUnescapedEnvVarsInternal(strippedSpan, result, depth + 1);
            result.Append('}');
            return closeIndex + 3;
        }

        result.Append(input[..(closeIndex + 3)]);
        return closeIndex + 3;
    }

    /// <summary>
    /// Handles ${VAR:-DEFAULT} after separator is found in stripped content
    /// </summary>
    private static void ProcessWithDefault(ReadOnlySpan<char> content, int sepIdx, StringBuilder result, int depth)
    {
        if (depth > MaxRecursionDepth)
        {
            throw new InvalidOperationException("Maximum recursion depth exceeded while processing environment variables");
        }

        if (content.Length > MaxContentLength)
        {
            throw new ArgumentException("Variable content exceeds maximum allowed length");
        }

        var nextDepth = depth + 1;
        var varPart = content[..sepIdx];
        var defaultPart = content[(sepIdx + 2)..];

        result.Append("$$").Append('{');
        EscapeUnescapedEnvVarsInternal(varPart, result, nextDepth);
        result.Append(":-");
        EscapeUnescapedEnvVarsInternal(defaultPart, result, nextDepth);
        result.Append('}');
    }

    /// <summary>
    /// Finds the default value separator ':-' in a variable expression.
    /// Properly handles nested expressions to find only top-level separators.
    /// 
    /// Examples of where it finds separators:
    /// - FOO:-bar -> finds at index 3
    /// - FOO${BAR:-baz}:-default -> finds at last position
    /// - FOO:-bar:-baz -> finds at first occurrence (index 3)
    /// </summary>
    private static int FindDefaultSeparator(ReadOnlySpan<char> content)
    {
        var depth = 0;
        for (var i = 0; i < content.Length - 1; i++)
        {
            if (content[i] == '$' && i + 1 < content.Length && content[i + 1] == '{')
            {
                if (i == 0 || content[i - 1] != '$')
                {
                    depth++;
                    i++; // Skip the next character since we've already checked it
                }
            }
            else if (content[i] == '}')
            {
                if (depth > 0)
                {
                    depth--;
                }
            }
            else if (depth == 0 && content[i] == ':' && i + 1 < content.Length && content[i + 1] == '-')
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Validates if the given string is a valid Docker environment variable name.
    /// A valid name must:
    /// 1. Not be empty
    /// 2. Start with a letter or underscore
    /// 3. Contain only letters, digits, or underscores
    /// 
    /// Examples:
    /// - Valid: "FOO", "_bar", "MY_VAR_123"
    /// - Invalid: "123foo" (starts with number), "my-var" (contains hyphen), "" (empty)
    /// </summary>
    /// <param name="name">The variable name to validate</param>
    /// <returns>true if the name is a valid Docker environment variable name; otherwise, false</returns>
    private static bool IsValidSimpleVariable(ReadOnlySpan<char> name)
    {
        if (name.IsEmpty || !IsLetterOrUnderscore(name[0]))
        {
            return false;
        }

        foreach (var c in name)
        {
            if (!char.IsLetterOrDigit(c) && c != '_')
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsLetterOrUnderscore(char ch)
    {
        return char.IsLetter(ch) || ch == '_';
    }
}