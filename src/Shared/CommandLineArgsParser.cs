// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Aspire.Hosting.Utils;

internal static class CommandLineArgsParser
{
    /// <summary>Parses a command-line argument string into a list of arguments.</summary>
    public static List<string> Parse(string arguments)
    {
        var result = new List<string>();
        ParseArgumentsIntoList(arguments, result);
        return result;
    }

    /// <summary>Parses a command-line argument string into a list of arguments.</summary>
    /// <param name="arguments">The argument string.</param>
    /// <param name="results">The list into which the component arguments should be stored.</param>
    /// <remarks>
    /// This follows the rules outlined in "Parsing C++ Command-Line Arguments" at
    /// https://msdn.microsoft.com/library/17w5ykft.aspx.
    /// </remarks>
    // copied from https://github.com/dotnet/runtime/blob/404b286b23093cd93a985791934756f64a33483e/src/libraries/System.Diagnostics.Process/src/System/Diagnostics/Process.Unix.cs#L846-L945
    private static void ParseArgumentsIntoList(string arguments, List<string> results)
    {
        // Iterate through all of the characters in the argument string.
        for (int i = 0; i < arguments.Length; i++)
        {
            while (i < arguments.Length && (arguments[i] == ' ' || arguments[i] == '\t'))
            {
                i++;
            }

            if (i == arguments.Length)
            {
                break;
            }

            results.Add(GetNextArgument(arguments, ref i));
        }
    }

    private static string GetNextArgument(string arguments, ref int i)
    {
        var currentArgument = new StringBuilder();
        bool inQuotes = false;

        while (i < arguments.Length)
        {
            // From the current position, iterate through contiguous backslashes.
            int backslashCount = 0;
            while (i < arguments.Length && arguments[i] == '\\')
            {
                i++;
                backslashCount++;
            }

            if (backslashCount > 0)
            {
                if (i >= arguments.Length || arguments[i] != '"')
                {
                    // Backslashes not followed by a double quote:
                    // they should all be treated as literal backslashes.
                    currentArgument.Append('\\', backslashCount);
                }
                else
                {
                    // Backslashes followed by a double quote:
                    // - Output a literal slash for each complete pair of slashes
                    // - If one remains, use it to make the subsequent quote a literal.
                    currentArgument.Append('\\', backslashCount / 2);
                    if (backslashCount % 2 != 0)
                    {
                        currentArgument.Append('"');
                        i++;
                    }
                }

                continue;
            }

            char c = arguments[i];

            // If this is a double quote, track whether we're inside of quotes or not.
            // Anything within quotes will be treated as a single argument, even if
            // it contains spaces.
            if (c == '"')
            {
                if (inQuotes && i < arguments.Length - 1 && arguments[i + 1] == '"')
                {
                    // Two consecutive double quotes inside an inQuotes region should result in a literal double quote
                    // (the parser is left in the inQuotes region).
                    // This behavior is not part of the spec of code:ParseArgumentsIntoList, but is compatible with CRT
                    // and .NET Framework.
                    currentArgument.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                i++;
                continue;
            }

            // If this is a space/tab and we're not in quotes, we're done with the current
            // argument, it should be added to the results and then reset for the next one.
            if ((c == ' ' || c == '\t') && !inQuotes)
            {
                break;
            }

            // Nothing special; add the character to the current argument.
            currentArgument.Append(c);
            i++;
        }

        return currentArgument.ToString();
    }
}
