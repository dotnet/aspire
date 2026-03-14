// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Aspire.Hosting.Foundry;

/// <summary>
/// ATS-friendly configuration for hosted agent publishing.
/// </summary>
/// <remarks>
/// This type exists because <see cref="HostedAgentConfiguration"/> mixes the simple settings that
/// polyglot hosts can configure with Azure SDK-specific types such as <c>AgentTool</c>,
/// <c>ContentFilterConfiguration</c>, and <c>ProtocolVersionRecord</c>. Exposing that richer type
/// through ATS would either leak Azure-specific implementation details into polyglot hosts or surface
/// properties that those hosts cannot meaningfully construct.
///
/// This DTO intentionally exposes only the stable ATS-compatible subset and maps those values back
/// into the richer <see cref="HostedAgentConfiguration"/> used by the .NET implementation.
/// </remarks>
[AspireExport(ExposeProperties = true)]
internal sealed class HostedAgentAtsConfiguration
{
    /// <summary>
    /// Gets or sets the description of the hosted agent.
    /// </summary>
    public string Description { get; set; } = "Python Hosted Agent";

    /// <summary>
    /// Gets the metadata associated with the hosted agent.
    /// </summary>
    public Dictionary<string, string> Metadata { get; } = new()
    {
        { "DeployedBy", "Aspire Hosting Framework" },
        { "DeployedOn", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) }
    };

    /// <summary>
    /// Gets or sets the CPU allocation for each hosted agent instance, in vCPU cores.
    /// </summary>
    public decimal? Cpu { get; set; }

    /// <summary>
    /// Gets or sets the memory allocation for each hosted agent instance, in GiB.
    /// </summary>
    public decimal? Memory { get; set; }

    /// <summary>
    /// Gets the environment variables to set in the hosted agent container.
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; } = new();

    /// <summary>
    /// Applies the ATS-friendly configuration to the runtime hosted agent configuration.
    /// </summary>
    /// <param name="image">The fully qualified container image name for the hosted agent.</param>
    /// <returns>A populated <see cref="HostedAgentConfiguration"/>.</returns>
    public HostedAgentConfiguration ToHostedAgentConfiguration(string image)
    {
        var configuration = new HostedAgentConfiguration(image)
        {
            Description = Description
        };

        if (Cpu is not null)
        {
            configuration.Cpu = Cpu.Value;
        }

        if (Memory is not null)
        {
            configuration.Memory = Memory.Value;
        }

        configuration.Metadata.Clear();
        foreach (var kvp in Metadata)
        {
            configuration.Metadata[kvp.Key] = kvp.Value;
        }

        configuration.EnvironmentVariables.Clear();
        foreach (var kvp in EnvironmentVariables)
        {
            configuration.EnvironmentVariables[kvp.Key] = kvp.Value;
        }

        return configuration;
    }
}
