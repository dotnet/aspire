// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Aspire;

internal static class InteractionHelpers
{
    // Chosen to balance between being long enough for most normal use but provides a default limit
    // to prevent possible abuse of interactions API.
    public const int DefaultMaxLength = 8000;

    public static int GetMaxLength(int? configuredInputLength)
    {
        // An unconfigured max length uses the default.
        if (configuredInputLength is null || configuredInputLength == 0)
        {
            return DefaultMaxLength;
        }

        return configuredInputLength.Value;
    }
}
