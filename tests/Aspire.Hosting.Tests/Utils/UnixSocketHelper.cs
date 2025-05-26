// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Tests.Utils;

internal static class UnixSocketHelper
{
    public static string GetBackchannelSocketPath()
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var aspireCliPath = Path.Combine(homeDirectory, ".aspire", "cli", "backchannels");

        if (!Directory.Exists(aspireCliPath))
        {
            Directory.CreateDirectory(aspireCliPath);
        }

        var uniqueSocketPathSegment = Guid.NewGuid().ToString("N");
        var socketPath = Path.Combine(aspireCliPath, $"cli.sock.{uniqueSocketPathSegment}");
        return socketPath;
    }

}
