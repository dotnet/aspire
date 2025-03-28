// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Devcontainers.Codespaces;

/// <summary>
/// GitHub Codespaces configuration values.
/// </summary>
internal class CodespacesOptions
{
    /// <summary>
    /// When set to true, the app host is running in a GitHub Codespace.
    /// </summary>
    /// <remarks>
    /// Maps to the CODESPACE environment variable.
    /// </remarks>
    public bool IsCodespace { get; set; }

    /// <summary>
    /// When set it is the domain suffix used when port forwarding services hosted on the Codespace.
    /// </summary>
    /// <remarks>
    /// Maps to the GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN environment variable.
    /// </remarks>
    [MemberNotNullWhen(true, nameof(IsCodespace))]
    public string? PortForwardingDomain { get; set; }

    /// <summary>
    /// When set it is the name of the GitHub Codespace in which the app host is running.
    /// </summary>
    /// <remarks>
    /// Maps to the CODESPACE_NAME environment variable.
    /// </remarks>
    [MemberNotNullWhen(true, nameof(IsCodespace))]
    public string? CodespaceName { get; set; }
}

internal class ConfigureCodespacesOptions(IConfiguration configuration) : IConfigureOptions<CodespacesOptions>
{
    private const string CodespacesEnvironmentVariable = "CODESPACES";
    private const string CodespaceNameEnvironmentVariable = "CODESPACE_NAME";
    private const string GitHubCodespacesPortForwardingDomain = "GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN";

    private string GetRequiredCodespacesConfigurationValue(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        return configuration.GetValue<string>(key) ?? throw new DistributedApplicationException($"Codespaces was detected but {key} environment missing.");
    }

    public void Configure(CodespacesOptions options)
    {
        if (!configuration.GetValue<bool>(CodespacesEnvironmentVariable, false))
        {
            options.IsCodespace = false;
            return;
        }

        options.IsCodespace = true;
        options.PortForwardingDomain = GetRequiredCodespacesConfigurationValue(GitHubCodespacesPortForwardingDomain);
        options.CodespaceName = GetRequiredCodespacesConfigurationValue(CodespaceNameEnvironmentVariable);
    }
}