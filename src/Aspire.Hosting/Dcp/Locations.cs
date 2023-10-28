// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Aspire.Hosting.Dcp;

internal sealed class Locations(string basePath)
{
    public string DcpTempDir => Path.Join(basePath, "aspire");

    public string DcpSessionDir => Path.Combine(DcpTempDir, "session", Environment.ProcessId.ToString(CultureInfo.InvariantCulture));

    public string DcpKubeconfigPath => Path.Combine(DcpSessionDir, "kubeconfig");

    public string DcpLogSocket => Path.Combine(DcpSessionDir, "output.sock");
}
