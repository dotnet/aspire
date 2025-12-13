// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.RegularExpressions;

namespace Aspire.Hosting.Execution;

/// <summary>
/// Default implementation of <see cref="ICommandLineParser"/> that uses a canonical grammar
/// across all platforms and rejects shell operators.
/// </summary>
internal sealed partial class CommandLineParser : ICommandLineParser
{
    // Shell operators that are not allowed
    private static readonly string[] s_shellOperators = ["|", "&&", "||", ";", ">", ">>", "<", "<<", "&"];

    [GeneratedRegex(@"\$\(|\$\{|`")]
    private static partial Regex CommandSubstitutionRegex();

    /// <inheritdoc />
    public (string FileName, IReadOnlyList<string> Args) Parse(string commandLine)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandLine);

        // Check for command substitution patterns
        if (CommandSubstitutionRegex().IsMatch(commandLine))
        {
            throw new InvalidOperationException(
                "Command substitution ($(...), ${...}, or backticks) is not supported. " +
                "Use IVirtualShell.RunAsync() to capture command output programmatically.");
        }

        var tokens = Tokenize(commandLine);

        if (tokens.Count == 0)
        {
            throw new InvalidOperationException("Command line is empty after parsing.");
        }

        // Check for shell operators in tokens
        foreach (var token in tokens)
        {
            if (Array.Exists(s_shellOperators, op => op == token))
            {
                throw new InvalidOperationException(
                    $"Shell operator '{token}' is not supported. " +
                    "Use the VirtualShell API methods for composing commands programmatically.");
            }
        }

        var fileName = tokens[0];
        var args = tokens.Count > 1
            ? tokens.Skip(1).ToList()
            : (IReadOnlyList<string>)[];

        return (fileName, args);
    }

    /// <summary>
    /// Tokenizes a command line string using Windows-style quoting rules.
    /// </summary>
    /// <remarks>
    /// This follows the rules outlined in "Parsing C++ Command-Line Arguments" at
    /// https://msdn.microsoft.com/library/17w5ykft.aspx
    /// </remarks>
    private static List<string> Tokenize(string commandLine)
    {
        var results = new List<string>();

        for (int i = 0; i < commandLine.Length; i++)
        {
            // Skip whitespace
            while (i < commandLine.Length && (commandLine[i] == ' ' || commandLine[i] == '\t'))
            {
                i++;
            }

            if (i >= commandLine.Length)
            {
                break;
            }

            results.Add(GetNextToken(commandLine, ref i));
        }

        return results;
    }

    private static string GetNextToken(string commandLine, ref int i)
    {
        var currentToken = new StringBuilder();
        bool inQuotes = false;

        while (i < commandLine.Length)
        {
            // Handle backslashes
            int backslashCount = 0;
            while (i < commandLine.Length && commandLine[i] == '\\')
            {
                i++;
                backslashCount++;
            }

            if (backslashCount > 0)
            {
                if (i >= commandLine.Length || commandLine[i] != '"')
                {
                    // Backslashes not followed by a double quote:
                    // they should all be treated as literal backslashes.
                    currentToken.Append('\\', backslashCount);
                }
                else
                {
                    // Backslashes followed by a double quote:
                    // - Output a literal slash for each complete pair of slashes
                    // - If one remains, use it to make the subsequent quote a literal.
                    currentToken.Append('\\', backslashCount / 2);
                    if (backslashCount % 2 != 0)
                    {
                        currentToken.Append('"');
                        i++;
                    }
                }

                continue;
            }

            char c = commandLine[i];

            // Handle double quotes
            if (c == '"')
            {
                if (inQuotes && i < commandLine.Length - 1 && commandLine[i + 1] == '"')
                {
                    // Two consecutive double quotes inside a quoted region
                    // result in a literal double quote
                    currentToken.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                i++;
                continue;
            }

            // If this is whitespace and we're not in quotes, we're done with this token
            if ((c == ' ' || c == '\t') && !inQuotes)
            {
                break;
            }

            // Regular character
            currentToken.Append(c);
            i++;
        }

        return currentToken.ToString();
    }
}
