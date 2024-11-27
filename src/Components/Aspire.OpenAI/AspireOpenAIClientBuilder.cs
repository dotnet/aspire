// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using System.Data.Common;
using Microsoft.Extensions.Hosting;
using OpenAI;

namespace Aspire.OpenAI;

/// <summary>
/// A builder for configuring an <see cref="OpenAIClient"/> service registration.
/// </summary>
public class AspireOpenAIClientBuilder
{
    private const string DeploymentKey = "Deployment";
    private const string ModelKey = "Model";

    /// <summary>
    /// Constructs a new instance of <see cref="AspireOpenAIClientBuilder"/>.
    /// </summary>
    /// <param name="hostBuilder">The <see cref="IHostApplicationBuilder"/> with which services are being registered.</param>
    /// <param name="connectionName">The name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="serviceKey">The service key used to register the <see cref="OpenAIClient"/> service, if any.</param>
    public AspireOpenAIClientBuilder(IHostApplicationBuilder hostBuilder, string connectionName, string? serviceKey)
    {
        HostBuilder = hostBuilder;
        ConnectionName = connectionName;
        ServiceKey = serviceKey;
    }

    /// <summary>
    /// Gets the <see cref="IHostApplicationBuilder"/> with which services are being registered.
    /// </summary>
    public IHostApplicationBuilder HostBuilder { get; }

    /// <summary>
    /// Gets the name used to retrieve the connection string from the ConnectionStrings configuration section.
    /// </summary>
    public string ConnectionName { get; }

    /// <summary>
    /// Gets the service key used to register the <see cref="OpenAIClient"/> service, if any.
    /// </summary>
    public string? ServiceKey { get; }

    /// <summary>
    /// Gets the name of the configuration section for this component type.
    /// </summary>
    public virtual string ConfigurationSectionName => AspireOpenAIExtensions.DefaultConfigSectionName;

    internal string GetRequiredDeploymentName()
    {
        string? deploymentName = null;

        var configuration = HostBuilder.Configuration;
        if (configuration.GetConnectionString(ConnectionName) is string connectionString)
        {
            var connectionBuilder = new DbConnectionStringBuilder { ConnectionString = connectionString };
            var deploymentValue = ConnectionStringValue(connectionBuilder, DeploymentKey);
            var modelValue = ConnectionStringValue(connectionBuilder, ModelKey);
            if (deploymentValue is not null && modelValue is not null)
            {
                throw new InvalidOperationException(
                    $"The connection string '{ConnectionName}' contains both '{DeploymentKey}' and '{ModelKey}' keys. Either of these may be specified, but not both.");
            }

            deploymentName = deploymentValue ?? modelValue;
        }

        if (string.IsNullOrEmpty(deploymentName))
        {
            var configSection = configuration.GetSection(ConfigurationSectionName);
            deploymentName = configSection[DeploymentKey];
        }

        if (string.IsNullOrEmpty(deploymentName))
        {
            throw new InvalidOperationException($"The deployment could not be determined. Ensure a '{DeploymentKey}' or '{ModelKey}' value is provided in 'ConnectionStrings:{ConnectionName}', or specify a '{DeploymentKey}' in the '{ConfigurationSectionName}' configuration section, or specify a '{nameof(deploymentName)}' in the call.");
        }

        return deploymentName;
    }

    private static string? ConnectionStringValue(DbConnectionStringBuilder connectionString, string key)
        => connectionString.TryGetValue(key, out var value) ? value as string : null;
}
