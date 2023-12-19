// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Utils;

internal static class PasswordUtil
{
    internal static string EscapePassword(string password) => password.Replace("\"", "\"\"");

    /// <summary>
    /// Returns a random password of length <paramref name="length"/> that does not contain any characters that would be escaped by <see cref="EscapePassword(string)"/>.
    /// </summary>
    /// <remarks>If <paramref name="length"/> is greater than or equal to four, the password is guaranteed to contain an uppercase ASCII letter, a lowercase ASCII letter, a digit, and a symbol.</remarks>
    internal static string GeneratePassword(int length = 20)
    {
        return string.Create(length, 0, (buffer, _) =>
        {
            if (length >= 1)
            {
                // add an uppercase ASCII letter
                Random.Shared.GetItems(PasswordChars.Slice(0, 26), buffer[..1]);

                if (length >= 2)
                {
                    // add a lowercase ASCII letter
                    Random.Shared.GetItems(PasswordChars.Slice(26, 26), buffer[1..2]);

                    if (length >= 3)
                    {
                        // add a digit
                        Random.Shared.GetItems(PasswordChars.Slice(52, 10), buffer[2..3]);

                        if (length >= 4)
                        {
                            // add a symbol
                            Random.Shared.GetItems(PasswordChars.Slice(62), buffer[3..4]);

                            if (length >= 5)
                            {
                                // use random password characters for the rest of the password
                                Random.Shared.GetItems(PasswordChars, buffer[4..]);
                            }
                        }
                    }
                }
            }
        });
    }

    // Characters to use in a password that do not need to be escaped. They should be in the order: UPPER, lower, digits, symbols
    private static ReadOnlySpan<char> PasswordChars => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmonpqrstuvwxyz0123456789!@$^&*()[]{}':,._-";
}
