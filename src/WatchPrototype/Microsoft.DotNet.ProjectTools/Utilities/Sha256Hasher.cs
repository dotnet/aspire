// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;

namespace Microsoft.DotNet.Utilities;

public static class Sha256Hasher
{
    /// <summary>
    /// The hashed mac address needs to be the same hashed value as produced by the other distinct sources given the same input. (e.g. VsCode)
    /// </summary>
    public static string Hash(string text)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    public static string HashWithNormalizedCasing(string text)
        => Hash(text.ToUpperInvariant());
}
