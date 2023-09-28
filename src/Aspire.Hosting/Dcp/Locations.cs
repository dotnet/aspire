// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.InteropServices;

namespace Aspire.Hosting.Dcp;

internal static class Locations
{
    public static string DcpDir
    {
        get
        {
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(homeDir, ".dcp");
        }
    }

    public static string DcpTempDir
    {
        get
        {
            return Path.Join(Path.GetTempPath(), "aspire");
        }
    }

    public static string DcpSessionDir => Path.Combine(DcpTempDir, "session", Environment.ProcessId.ToString(CultureInfo.InvariantCulture));

    public static string DcpKubeconfigPath => Path.Combine(DcpSessionDir, "kubeconfig");

    public static string DcpLogSocket => Path.Combine(DcpSessionDir, "output.sock");

    public static string DcpCliPath
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Path.Combine(DcpDir, "dcp.exe");
            }
            else
            {
                return Path.Combine(DcpDir, "dcp");
            }
        }
    }
}
