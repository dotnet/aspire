// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Aspire.Hosting.Utils;

internal static class RuntimeUtils
{
    public static bool TryGetVersion([NotNullWhen(true)] out Version? version)
    {
        // The framework description is in the format ".NET <version>"
        var description = RuntimeInformation.FrameworkDescription;
        var parts = description.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2 && Version.TryParse(parts[1], out version))
        {
            return true;
        }

        version = null;
        return false;
    }
}