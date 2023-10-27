// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.Dcp;

public sealed class DcpOptions
{
    private const string DcpCliPathMetadataKey = "DcpCliPath";
    private const string DcpExtensionsPathMetadataKey = "DcpExtensionsPath";
    private const string DcpBinPathMetadataKey = "DcpBinPath";

    public static string DcpPublisher = nameof(DcpPublisher);

    /// <summary>
    /// The path to the DCP executable used for Aspire orchestration
    /// </summary>
    /// <example>
    /// C:\Program Files\dotnet\packs\Aspire.Hosting.Orchestration.win-x64\8.0.0-preview.1.23518.6\tools\dcp.exe
    /// </example>
    public string? CliPath { get; set; }

    /// <summary>
    /// Optional path to a folder container the DCP extension assemblies (dcpd, dcpctrl, etc.)
    /// </summary>
    /// <example>
    /// C:\Program Files\dotnet\packs\Aspire.Hosting.Orchestration.win-x64\8.0.0-preview.1.23518.6\tools\ext\
    /// </example>
    public string? ExtensionsPath { get; set; }

    /// <summary>
    /// Optional path to a folder containing additional DCP binaries (traefik, etc.)
    /// </summary>
    /// <example>
    /// C:\Program Files\dotnet\packs\Aspire.Hosting.Orchestration.win-x64\8.0.0-preview.1.23518.6\tools\ext\bin\
    /// </example>
    public string? BinPath { get; set; }

    public void ApplyApplicationConfiguration(DistributedApplicationOptions appOptions, IConfiguration configuration)
    {
        if (!string.IsNullOrEmpty(configuration[nameof(CliPath)]))
        {
            // If an explicit path to DCP was provided from configuration, don't try to resolve via assembly attributes
            CliPath = configuration[nameof(CliPath)];
        }
        else
        {
            // Calculate DCP locations from configuration options
            Assembly? appHostAssembly = Assembly.GetEntryAssembly();
            if (!string.IsNullOrEmpty(appOptions.AssemblyName))
            {
                try
                {
                    // Find an assembly in the current AppDomain with the given name
                    appHostAssembly = Assembly.Load(appOptions.AssemblyName);
                    Console.WriteLine(appHostAssembly?.FullName);
                    if (appHostAssembly == null)
                    {
                        throw new FileNotFoundException("No assembly with name '{appOptions.AssemblyName}' exists in the current AppDomain.");
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to load AppHost assembly '{appOptions.AssemblyName}' specified in {nameof(DistributedApplicationOptions)}.", ex);
                }
            }

            
            IEnumerable<AssemblyMetadataAttribute>? assemblyMetadata = appHostAssembly?.GetCustomAttributes<AssemblyMetadataAttribute>();
            CliPath = GetMetadataValue(assemblyMetadata, DcpCliPathMetadataKey);
            ExtensionsPath = GetMetadataValue(assemblyMetadata, DcpExtensionsPathMetadataKey);
            BinPath = GetMetadataValue(assemblyMetadata, DcpBinPathMetadataKey);
        }

        if (string.IsNullOrEmpty(CliPath))
        {
            throw new InvalidOperationException($"Could not resolve the path to the Aspire application host. The application cannot be run without it.");
        }
    }

    private static string? GetMetadataValue(IEnumerable<AssemblyMetadataAttribute>? assemblyMetadata, string key)
    {
        return assemblyMetadata?.FirstOrDefault(m => string.Equals(m.Key, key, StringComparison.OrdinalIgnoreCase))?.Value;
    }
}
