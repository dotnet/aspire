// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography;

namespace Aspire.Hosting.Utils;

internal static class PasswordGenerator
{
    // Some chars are excluded:
    // - prevent potential confusions, e.g., 0,o,O and i,I,l
    // - exclude special chars which could interfere with command line arguments or connection strings, e.g. =,$,...

    internal const string LowerCaseChars = "abcdefghjkmnpqrstuvwxyz"; // exclude i,l,o
    internal const string UpperCaseChars = "ABCDEFGHJKMNPQRSTUVWXYZ"; // exclude I,L,O
    internal const string DigitChars = "0123456789";
    internal const string SpecialChars = "-_#@!./:[]{}+*()~"; // exclude &<>=;,`'^%$

    /// <summary>
    /// Creates a cryptographically random password.
    /// </summary>
    /// <param name="lowerCase">The number of lowercase chars in the generated password or 0 to ignore this component.</param>
    /// <param name="upperCase">The number of uppercase chars in the generated password or 0 to ignore this component.</param>
    /// <param name="digit">The number of digits in the generated password or 0 to ignore this component.</param>
    /// <param name="special">The number of special chars in the generated password or 0 to ignore this component.</param>
    /// <returns>A cryptographically random password.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If lowerCase, upperCase, digit or special is negative or their sum is zero.</exception>
    public static string GeneratePassword(int lowerCase, int upperCase, int digit, int special)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(lowerCase);
        ArgumentOutOfRangeException.ThrowIfNegative(upperCase);
        ArgumentOutOfRangeException.ThrowIfNegative(digit);
        ArgumentOutOfRangeException.ThrowIfNegative(special);

        var length = lowerCase + upperCase + digit + special;

        ArgumentOutOfRangeException.ThrowIfZero(length);

        Debug.Assert(length <= 128, "password too long");

        Span<char> chars = stackalloc char[length];

        RandomNumberGenerator.GetItems(LowerCaseChars, chars.Slice(0, lowerCase));
        RandomNumberGenerator.GetItems(UpperCaseChars, chars.Slice(lowerCase, upperCase));
        RandomNumberGenerator.GetItems(DigitChars, chars.Slice(lowerCase + upperCase, digit));
        RandomNumberGenerator.GetItems(SpecialChars, chars.Slice(lowerCase + upperCase + digit, special));
        RandomNumberGenerator.Shuffle(chars);

        return new string(chars);
    }
}
