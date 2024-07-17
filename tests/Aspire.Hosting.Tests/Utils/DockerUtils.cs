// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.Utils;

public sealed class DockerUtils
{
    public static void AttemptDeleteDockerVolume(string volumeName)
    {
        for (var i = 0; i < 3; i++)
        {
            if (i != 0)
            {
                Thread.Sleep(1000);
            }

            if (Process.Start("docker", $"volume rm {volumeName}") is { } process)
            {
                var exited = process.WaitForExit(TimeSpan.FromSeconds(3));
                var done = exited && process.ExitCode == 0;
                process.Kill(entireProcessTree: true);
                process.Dispose();

                if (done)
                {
                    break;
                }
            }
        }
    }
}
