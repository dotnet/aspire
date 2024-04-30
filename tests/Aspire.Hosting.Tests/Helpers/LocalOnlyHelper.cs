// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Tests.Helpers;

internal static class LocalOnlyHelper
{
    public static string SkipIfRunningInCI(string attributeName, params string[] executablesOnPath)
    {
        // BUILD_BUILDID is defined by Azure Dev Ops

        if (Environment.GetEnvironmentVariable("BUILD_BUILDID") != null)
        {
            return $"{attributeName} tests are not run as part of CI.";
        }

        foreach (var executable in executablesOnPath)
        {
            if (FileUtil.FindFullPathFromPath(executable) is null)
            {
                return $"Unable to locate {executable} on the PATH";
            }
        }

        return null!;
    }
}
