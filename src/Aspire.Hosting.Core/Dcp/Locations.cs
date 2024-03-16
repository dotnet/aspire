// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dcp;

internal sealed class Locations
{
    private string? _basePath;

    public string DcpSessionDir => GetOrCreateBasePath();

    public string DcpKubeconfigPath => Path.Combine(DcpSessionDir, "kubeconfig");

    public string DcpLogSocket => Path.Combine(DcpSessionDir, "output.sock");

    private string GetOrCreateBasePath()
    {
        _basePath ??= Directory.CreateTempSubdirectory("aspire.").FullName;
        return _basePath;
    }
}
