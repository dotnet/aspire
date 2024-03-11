// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Utils;

internal static class PathNormalizer
{
    public static string NormalizePathForCurrentPlatform(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        // Fix slashes
        path = path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

        return Path.GetFullPath(path);
    }

    public static string NormalizePathForManifest(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        // Fix slashes
        path = path.Replace('\\', '/');

        // Prepend ./ if the path is not fully qualified, rooted, or relative qualified
        if (!Path.IsPathFullyQualified(path) && !Path.IsPathRooted(path) && !IsPathRelativeQualified(path))
        {
            path = "./" + path;
        }

        return path;
    }

    private static bool IsPathRelativeQualified(string path)
    {
        return path.StartsWith("./") || path.StartsWith("../");
    }
}
