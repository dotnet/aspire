// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Aspire.Hosting.Dcp;

internal static class Locations
{
    private const string DcpCliPathMetadataKey = "dcpclipath";
    private const string DcpExtensionsPathMetadataKey = "dcpextensionspath";
    private const string DcpBinPathMetadataKey = "dcpbinpath";

    private static readonly Lazy<IEnumerable<AssemblyMetadataAttribute>?> s_assemblyMetadata = new Lazy<IEnumerable<AssemblyMetadataAttribute>?>(() =>
    {
        Assembly? assembly = Assembly.GetEntryAssembly();
        return assembly?.GetCustomAttributes<AssemblyMetadataAttribute>();
    });

    private static readonly Lazy<string> s_dcpCliPath = new Lazy<string>(() =>
    {
        string? dcpCliPath = s_assemblyMetadata.Value?.FirstOrDefault(m => string.Equals(m.Key, DcpCliPathMetadataKey, StringComparison.OrdinalIgnoreCase))?.Value;

        if (dcpCliPath != null)
        {
            return dcpCliPath;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Path.Combine(DcpDir, "dcp.exe");
        }
        else
        {
            return Path.Combine(DcpDir, "dcp");
        }
    });

    private static readonly Lazy<string?> s_dcpExtensionsPath = new Lazy<string?>(() =>
    {
        return s_assemblyMetadata.Value?.FirstOrDefault(m => string.Equals(m.Key, DcpExtensionsPathMetadataKey, StringComparison.OrdinalIgnoreCase))?.Value;
    });

    private static readonly Lazy<string?> s_dcpBinPath = new Lazy<string?>(() =>
    {
        return s_assemblyMetadata.Value?.FirstOrDefault(m => string.Equals(m.Key, DcpBinPathMetadataKey, StringComparison.OrdinalIgnoreCase))?.Value;
    });

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

    public static string DcpCliPath => s_dcpCliPath.Value;

    public static string? DcpExtensionsPath => s_dcpExtensionsPath.Value;

    public static string? DcpBinPath => s_dcpBinPath.Value;
}
