// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Aspire.Hosting.Utils;

internal static class TokenGenerator
{
    public static string GenerateToken()
    {
        var s = PasswordGenerator.Generate(minLength: 24, lower: true, upper: true, numeric: true, special: true, minLower: 0, minUpper: 0, minNumeric: 0, minSpecial: 0);
        var bytes = Encoding.UTF8.GetBytes(s);

#if NET9_0
        return Convert.ToHexStringLower(bytes);
#else
        return Convert.ToHexString(bytes).ToLower();
#endif
    }
}
