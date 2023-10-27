// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.Dcp;

public sealed class DcpOptions
{
    private const string DcpCliPathMetadataKey = "dcpclipath";
    private const string DcpExtensionsPathMetadataKey = "dcpextensionspath";
    private const string DcpBinPathMetadataKey = "dcpbinpath";

    public static string DCP = nameof(DCP);

    public string? DcpCliPath { get; set; }

    public string? DcpExtensionsPath { get; set; }

    public string? DcpBinPath { get; set; }

    public void ApplyApplicationConfiguration(DistributedApplicationOptions appOptions, IConfiguration configuration)
    {
        if (!string.IsNullOrEmpty(configuration["Dcp"]))
        {
            // If an explicit path to DCP was provided from configuration, don't try to resolve via assembly attributes
            DcpCliPath = configuration["Dcp"];
        }
        else
        {
            // Calculate DCP locations from configuration options
            Assembly? appHostAssembly;
            if (!string.IsNullOrEmpty(appOptions.AssemblyName))
            {
                try
                {
                    // Find an assembly in the current AppDomain with the given name
                    appHostAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => string.Equals(assembly.GetName().Name, appOptions.AssemblyName, StringComparison.Ordinal));
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

            appHostAssembly = Assembly.GetEntryAssembly();
            IEnumerable<AssemblyMetadataAttribute>? assemblyMetadata = appHostAssembly?.GetCustomAttributes<AssemblyMetadataAttribute>();
            DcpCliPath = GetMetadataValue(assemblyMetadata, DcpCliPathMetadataKey);
            DcpExtensionsPath = GetMetadataValue(assemblyMetadata, DcpExtensionsPathMetadataKey);
            DcpBinPath = GetMetadataValue(assemblyMetadata, DcpBinPathMetadataKey);
        }

        if (string.IsNullOrEmpty(DcpCliPath))
        {
            throw new InvalidOperationException($"Could not resolve the path to the Aspire application host. The application cannot be run without it.");
        }
    }

    private static string? GetMetadataValue(IEnumerable<AssemblyMetadataAttribute>? assemblyMetadata, string key)
    {
        return assemblyMetadata?.FirstOrDefault(m => string.Equals(m.Key, key, StringComparison.OrdinalIgnoreCase))?.Value;
    }
}
