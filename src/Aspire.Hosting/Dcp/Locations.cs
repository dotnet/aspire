// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Aspire.Hosting.Dcp;

internal static class Locations
{
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
}
