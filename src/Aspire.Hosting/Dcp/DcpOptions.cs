// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.Dcp;

internal sealed class DcpOptions
{
    private const string DcpCliPathMetadataKey = "DcpCliPath";
    private const string DcpExtensionsPathMetadataKey = "DcpExtensionsPath";
    private const string DcpBinPathMetadataKey = "DcpBinPath";
    private const string DashboardPathMetadataKey = "aspiredashboardpath";

    public static string DcpPublisher = nameof(DcpPublisher);

    /// <summary>
    /// The path to the DCP executable used for Aspire orchestration
    /// </summary>
    /// <example>
    /// C:\Program Files\dotnet\packs\Aspire.Hosting.Orchestration.win-x64\8.0.0-preview.1.23518.6\tools\dcp.exe
    /// </example>
    public string? CliPath { get; set; }

    /// <summary>
    /// Optional path to a folder containing the DCP extension assemblies (dcpd, dcpctrl, etc.).
    /// </summary>
    /// <example>
    /// C:\Program Files\dotnet\packs\Aspire.Hosting.Orchestration.win-x64\8.0.0-preview.1.23518.6\tools\ext\
    /// </example>
    public string? ExtensionsPath { get; set; }

    /// <summary>
    /// Optional path to a folder containing the Aspire Dashboard binaries.
    /// </summary>
    /// <example>
    /// When running the playground applications in this repo: <c>..\..\..\artifacts\bin\Aspire.Dashboard\Debug\net9.0\Aspire.Dashboard.dll</c>
    /// </example>
    public string? DashboardPath { get; set; }

    /// <summary>
    /// Optional path to a folder containing additional DCP binaries.
    /// </summary>
    /// <example>
    /// C:\Program Files\dotnet\packs\Aspire.Hosting.Orchestration.win-x64\8.0.0-preview.1.23518.6\tools\ext\bin\
    /// </example>
    public string? BinPath { get; set; }

    /// <summary>
    /// Optional container runtime to override default runtime for DCP containers.
    /// </summary>
    /// <example>
    /// podman
    /// </example>
    public string? ContainerRuntime { get; set; }

    /// <summary>
    /// How long the dependency check will wait (in seconds) for a response before timing out.
    /// Timeout is disabled if set to zero or a negative value.
    /// </summary>
    public int DependencyCheckTimeout { get; set; } = 25;

    public void ApplyApplicationConfiguration(DistributedApplicationOptions appOptions, IConfiguration dcpPublisherConfiguration, IConfiguration publishingConfiguration, IConfiguration coreConfiguration)
    {
        string? publisher = publishingConfiguration[nameof(PublishingOptions.Publisher)];
        if (publisher is not null && publisher != "dcp")
        {
            // If DCP is not set as the publisher, don't calculate the DCP config
            return;
        }

        if (!string.IsNullOrEmpty(dcpPublisherConfiguration[nameof(CliPath)]))
        {
            // If an explicit path to DCP was provided from configuration, don't try to resolve via assembly attributes
            CliPath = dcpPublisherConfiguration[nameof(CliPath)];
        }
        else
        {
            var assemblyMetadata = appOptions.Assembly?.GetCustomAttributes<AssemblyMetadataAttribute>();
            CliPath = GetMetadataValue(assemblyMetadata, DcpCliPathMetadataKey);
            ExtensionsPath = GetMetadataValue(assemblyMetadata, DcpExtensionsPathMetadataKey);
            BinPath = GetMetadataValue(assemblyMetadata, DcpBinPathMetadataKey);
            DashboardPath = GetMetadataValue(assemblyMetadata, DashboardPathMetadataKey);
        }

        if (!string.IsNullOrEmpty(dcpPublisherConfiguration[nameof(ContainerRuntime)]))
        {
            ContainerRuntime = dcpPublisherConfiguration[nameof(ContainerRuntime)];
        }
        else
        {
            ContainerRuntime = coreConfiguration.GetValue<string>("DOTNET_ASPIRE_CONTAINER_RUNTIME");
        }

        if (!string.IsNullOrEmpty(dcpPublisherConfiguration[nameof(DependencyCheckTimeout)]))
        {
            if (int.TryParse(dcpPublisherConfiguration[nameof(DependencyCheckTimeout)], out var timeout))
            {
                DependencyCheckTimeout = timeout;
            }
            else
            {
                throw new InvalidOperationException($"Invalid value \"{dcpPublisherConfiguration[nameof(DependencyCheckTimeout)]}\" for \"--dependency-check-timeout\". Exepcted an integer value.");
            }
        }
        else
        {
            DependencyCheckTimeout = coreConfiguration.GetValue<int>("DOTNET_ASPIRE_DEPENDENCY_CHECK_TIMEOUT", DependencyCheckTimeout);
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
