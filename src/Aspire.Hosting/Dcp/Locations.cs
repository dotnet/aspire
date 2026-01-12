// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREFILESYSTEM001 // Type is for evaluation purposes only

namespace Aspire.Hosting.Dcp;

internal sealed class Locations
{
    private readonly IFileSystemService _directoryService;
    private string? _dcpSessionDir;
    private readonly string? _externalKubeconfigPath;

    public Locations(IFileSystemService directoryService)
    {
        _directoryService = directoryService;

        // Check for CLI-owned DCP mode via environment variable
        var externalKubeconfig = Environment.GetEnvironmentVariable("DCP_KUBECONFIG_PATH");
        if (!string.IsNullOrEmpty(externalKubeconfig))
        {
            _externalKubeconfigPath = externalKubeconfig;
        }
    }

    /// <summary>
    /// Gets whether DCP is externally managed (CLI-owned mode).
    /// When true, AppHost should not launch or stop DCP - the CLI owns the DCP lifecycle.
    /// </summary>
    public bool IsExternalDcp => _externalKubeconfigPath != null;

    public string DcpSessionDir => GetOrCreateDcpSessionDir();

    public string DcpKubeconfigPath => _externalKubeconfigPath ?? Path.Combine(DcpSessionDir, "kubeconfig");

    public string DcpLogSocket => Path.Combine(DcpSessionDir, "output.sock");

    private string GetOrCreateDcpSessionDir()
    {
        if (_dcpSessionDir == null)
        {
            // Use the temp directory service to create a DCP-specific subdirectory
            _dcpSessionDir = _directoryService.TempDirectory.CreateTempSubdirectory("aspire-dcp").Path;
        }

        return _dcpSessionDir;
    }
}
