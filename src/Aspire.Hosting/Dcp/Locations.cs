// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dcp;

internal sealed class Locations(string basePath)
{
    public string DcpSessionDir => basePath;

    public string DcpKubeconfigPath => Path.Combine(DcpSessionDir, "kubeconfig");

    public string DcpLogSocket => Path.Combine(DcpSessionDir, "output.sock");
}
