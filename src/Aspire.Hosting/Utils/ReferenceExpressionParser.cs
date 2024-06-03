// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Aspire.Hosting.Utils;
internal static partial class ReferenceExpressionParser
{
    // Formatting rules taken from StringBuilder.AppendFormat
    internal static string ParseFormat(string format)
    {
        int pos = 0;
        int bracePos;
        char ch;

        StringBuilder parsedFormat = new StringBuilder(format.Length);
        while (true)
        {
            while (true)
            {
                if (pos >= format.Length)
                {
                    return parsedFormat.ToString();
                }

                ReadOnlySpan<char> remainder = format.AsSpan(pos);
                int countUntilNextBrace = remainder.IndexOfAny('{', '}');
                if (countUntilNextBrace < 0)
                {
                    parsedFormat.Append(remainder);
                    return parsedFormat.ToString();
                }
                parsedFormat.Append(remainder.Slice(0, countUntilNextBrace));
                pos += countUntilNextBrace;

                char brace = format[pos];
                bracePos = pos;

                ch = MoveNext(format, ref pos);

                // If the brace was at the end of the string, add it twice to escape it.
                if (ch == '\0')
                {
                    parsedFormat.Append(brace, 2);
                    return parsedFormat.ToString();
                }

                // If the next character is the same brace, it is already escaped, so we keep it as is.
                if (brace == ch)
                {
                    parsedFormat.Append(brace, 2);
                    pos++;
                    continue;
                }

                if (brace != '{' && ch != brace)
                {
                    // If the brace was not an opening brace, it was a closing brace
                    // and needs to be escaped, if it isn't already, so add it twice.
                    parsedFormat.Append(brace, 2);
                    continue;
                }

                // A brace was found, and need to parse based off the next character(s)
                break;
            }

            int index = ch - '0';
            if ((uint)index >= 10u)
            {
                // If the character after the brace is not a digit, it's not a parameter.
                // Add the brace twice to escape it.
                parsedFormat.Append('{', 2);
                continue;
            }

            ch = MoveNext(format, ref pos);
            // If the character was at the end of the string, add the beginning brace twice to escape it.
            if (ch == '\0')
            {
                parsedFormat.Append('{', 2);
                return parsedFormat.ToString();
            }

            if (ch != '}')
            {
                // Parameter starts with a digit
                while (char.IsAsciiDigit(ch))
                {
                    index = index * 10 + ch - '0';
                    ch = MoveNext(format, ref pos);
                    if (ch == '\0')
                    {
                        parsedFormat.Append('{');
                        parsedFormat.Append(format.AsSpan(bracePos));
                        return parsedFormat.ToString();
                    }
                }

                // Parameter can contain whitespace
                while (ch == ' ')
                {
                    ch = MoveNext(format, ref pos);
                    if (ch == '\0')
                    {
                        parsedFormat.Append('{');
                        parsedFormat.Append(format.AsSpan(bracePos));
                        return parsedFormat.ToString();
                    }
                }

                // optional argument:
                //  comma,
                //  optional whitespace,
                //  optional -,
                //  at least one digit,
                //  optional whitespace
                if (ch == ',')
                {
                    // Parse whitespace
                    do
                    {
                        ch = MoveNext(format, ref pos);
                        if (ch == '\0')
                        {
                            parsedFormat.Append('{');
                            parsedFormat.Append(format.AsSpan(bracePos));
                            return parsedFormat.ToString();
                        }
                    }
                    while (ch == ' ');

                    if (ch == '-')
                    {
                        ch = MoveNext(format, ref pos);
                        if (ch == '\0')
                        {
                            parsedFormat.Append('{');
                            parsedFormat.Append(format.AsSpan(bracePos));
                            return parsedFormat.ToString();
                        }
                    }

                    ch = MoveNext(format, ref pos);
                    if (ch == '\0')
                    {
                        parsedFormat.Append('{');
                        parsedFormat.Append(format.AsSpan(bracePos));
                        return parsedFormat.ToString();
                    }

                    while (char.IsAsciiDigit(ch))
                    {
                        index = index * 10 + ch - '0';
                        ch = MoveNext(format, ref pos);
                        if (ch == '\0')
                        {
                            parsedFormat.Append('{');
                            parsedFormat.Append(format.AsSpan(bracePos));
                            return parsedFormat.ToString();
                        }
                    }

                    while (ch == ' ')
                    {
                        ch = MoveNext(format, ref pos);
                        if (ch == '\0')
                        {
                            parsedFormat.Append('{');
                            parsedFormat.Append(format.AsSpan(bracePos));
                            return parsedFormat.ToString();
                        }
                    }
                }

                // Can be close brace or colon for formatting
                if (ch != '}')
                {
                    if (ch != ':')
                    {
                        // Parameter is not closed
                        parsedFormat.Append('{');
                        parsedFormat.Append(format.AsSpan(bracePos, pos));
                        continue;
                    }

                    while (true)
                    {
                        ch = MoveNext(format, ref pos);
                        if (ch == '\0')
                        {
                            parsedFormat.Append('{');
                            parsedFormat.Append(format.AsSpan(bracePos));
                            return parsedFormat.ToString();
                        }

                        // End of parameter argument found
                        if (ch == '}')
                        {
                            break;
                        }

                        // If another start brace is found, need to escape it and the existing start brace
                        if (ch == '{')
                        {
                            parsedFormat.Append('{');
                            parsedFormat.Append(format.AsSpan(bracePos, pos));
                            parsedFormat.Append('{');
                            break;
                        }
                    }
                }
            }

            pos++;
            // If position is at the end of the string, 
            if (pos == format.Length)
            {
                parsedFormat.Append(format.AsSpan(bracePos));
            }
            else
            {

                parsedFormat.Append(format.AsSpan(bracePos, pos));
            }
        }
    }

    private static char MoveNext(string format, ref int pos)
    {
        pos++;

        if (pos >= format.Length)
        {
            return '\0';
        }

        return format[pos];
    }
}
