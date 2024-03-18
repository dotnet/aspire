// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using static Aspire.Hosting.Utils.FileNameSuffixes;

namespace Aspire.Hosting.Utils;

internal static class FileUtil
{
    public static string FindFullPathFromPath(string command) => FindFullPathFromPath(command, Environment.GetEnvironmentVariable("PATH"), FileNameSuffixes.CurrentPlatform, Path.PathSeparator, File.Exists);

    internal static string FindFullPathFromPath(string command, string? pathVariable, PlatformFileNameSuffixes suffixes, char pathSeparator, Func<string, bool> fileExists)
    {
        Debug.Assert(!string.IsNullOrWhiteSpace(command));

        if (suffixes.Exe != "" && !command.EndsWith(suffixes.Exe))
        {
            command += suffixes.Exe;
        }

        foreach (var directory in (pathVariable ?? string.Empty).Split(pathSeparator))
        {
            var fullPath = Path.Combine(directory, command);

            if (fileExists(fullPath))
            {
                return fullPath;
            }
        }

        return command;
    }
}
