// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.Utils;

public sealed class DockerUtils
{
    public static void AttemptDeleteDockerVolume(string volumeName)
    {
        if (Process.Start("docker", $"volume rm {volumeName}") is { } process)
        {
            process.WaitForExit(TimeSpan.FromSeconds(3));
            process.Kill(entireProcessTree: true);
            process.Dispose();
        }
    }
}
