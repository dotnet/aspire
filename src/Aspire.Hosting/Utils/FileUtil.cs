// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Utils;

internal static class FileUtil
{
    public static string FindFullPathFromPath(string command)
    {
        ArgumentOutOfRangeException.ThrowIfNullOrWhiteSpace(command, nameof(command));

        if (FileNameSuffixes.CurrentPlatform.Exe != "" && !command.EndsWith(FileNameSuffixes.CurrentPlatform.Exe))
        {
            command = command + FileNameSuffixes.CurrentPlatform.Exe;
        }

        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator))
        {
            var fullPath = Path.Combine(directory, command + FileNameSuffixes.CurrentPlatform.Exe);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return command;
    }

}
