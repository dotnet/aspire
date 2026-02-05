// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Watch;

internal static class PathUtilities
{
    public static readonly IEqualityComparer<string?> OSSpecificPathComparer = Path.DirectorySeparatorChar == '\\' ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    public static readonly StringComparison OSSpecificPathComparison = Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public static string ExecutableExtension
        => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "";

    public static string EnsureTrailingSlash(string path)
        => (path is [.., var last] && last != Path.DirectorySeparatorChar) ? path + Path.DirectorySeparatorChar : path;

    public static string NormalizeDirectorySeparators(string path)
        => path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

    public static bool ContainsPath(IReadOnlySet<string> directories, string fullPath)
    {
        if (directories.Count == 0)
        {
            return false;
        }

        fullPath = Path.TrimEndingDirectorySeparator(fullPath);

        while (true)
        {
            if (directories.Contains(fullPath))
            {
                return true;
            }

            var containingDir = Path.GetDirectoryName(fullPath);
            if (containingDir == null)
            {
                return false;
            }

            fullPath = containingDir;
        }
    }

    public static IEnumerable<string> GetContainingDirectories(string path)
    {
        while (true)
        {
            var containingDir = Path.GetDirectoryName(path);
            if (containingDir == null)
            {
                yield break;
            }

            yield return containingDir;
            path = containingDir;
        }
    }
}
