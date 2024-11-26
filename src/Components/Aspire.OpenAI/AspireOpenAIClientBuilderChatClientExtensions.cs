// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using Aspire.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering <see cref="IChatClient"/> as a singleton in the services provided by the <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class AspireOpenAIClientBuilderChatClientExtensions
{
    private const string DeploymentKey = "Deployment";
    private const string ModelKey = "Model";

    /// <summary>
    /// Registers a singleton <see cref="IChatClient"/> in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">An <see cref="AspireOpenAIClientBuilder" />.</param>
    /// <param name="deploymentName">Optionally specifies which model deployment to use. If not specified, a value will be taken from the connection string.</param>
    /// <param name="disableOpenTelemetry">Optional. If <see langword="true"/>, skips registering open telemetry support in the <see cref="IChatClient"/> pipeline.</param>
    /// <returns>A <see cref="ChatClientBuilder"/> that can be used to build a pipeline around the inner <see cref="IChatClient"/>.</returns>
    /// <remarks>Reads the configuration from "Aspire.OpenAI" section.</remarks>
    public static ChatClientBuilder AddChatClient(
        this AspireOpenAIClientBuilder builder,
        string? deploymentName = null,
        bool disableOpenTelemetry = false)
    {
        return builder.HostBuilder.Services.AddChatClient(
            services => CreateInnerChatClient(services, builder, deploymentName, disableOpenTelemetry));
    }

    /// <summary>
    /// Registers a keyed singleton <see cref="IChatClient"/> in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">An <see cref="AspireOpenAIClientBuilder" />.</param>
    /// <param name="serviceKey">The service key with which the <see cref="IChatClient"/> will be registered.</param>
    /// <param name="deploymentName">Optionally specifies which model deployment to use. If not specified, a value will be taken from the connection string.</param>
    /// <param name="disableOpenTelemetry">Optional. If <see langword="true"/>, skips registering open telemetry support in the <see cref="IChatClient"/> pipeline.</param>
    /// <returns>A <see cref="ChatClientBuilder"/> that can be used to build a pipeline around the inner <see cref="IChatClient"/>.</returns>
    /// <remarks>Reads the configuration from "Aspire.OpenAI" section.</remarks>
    public static ChatClientBuilder AddKeyedChatClient(
        this AspireOpenAIClientBuilder builder,
        string serviceKey,
        string? deploymentName = null,
        bool disableOpenTelemetry = false)
    {
        return builder.HostBuilder.Services.AddKeyedChatClient(
            serviceKey,
            services => CreateInnerChatClient(services, builder, deploymentName, disableOpenTelemetry));
    }

    private static IChatClient CreateInnerChatClient(
        IServiceProvider services,
        AspireOpenAIClientBuilder builder,
        string? deploymentName,
        bool disableOpenTelemetry)
    {
        var openAiClient = builder.ServiceKey is null
            ? services.GetRequiredService<OpenAIClient>()
            : services.GetRequiredKeyedService<OpenAIClient>(builder.ServiceKey);

        deploymentName ??= GetRequiredDeploymentName(builder.HostBuilder.Configuration, builder.ConnectionName);
        var result = openAiClient.AsChatClient(deploymentName);

        return disableOpenTelemetry
            ? result
            : new OpenTelemetryChatClient(result);
    }

    private static string GetRequiredDeploymentName(IConfiguration configuration, string connectionName)
    {
        string? deploymentName = null;

        if (configuration.GetConnectionString(connectionName) is string connectionString)
        {
            var connectionBuilder = new DbConnectionStringBuilder { ConnectionString = connectionString };
            var deploymentValue = ConnectionStringValue(connectionBuilder, DeploymentKey);
            var modelValue = ConnectionStringValue(connectionBuilder, ModelKey);
            if (deploymentValue is not null && modelValue is not null)
            {
                throw new InvalidOperationException(
                    $"The connection string '{connectionName}' contains both '{DeploymentKey}' and '{ModelKey}' keys. Either of these may be specified, but not both.");
            }

            deploymentName = deploymentValue ?? modelValue;
        }

        var configurationSectionName = AspireOpenAIExtensions.DefaultConfigSectionName;
        if (string.IsNullOrEmpty(deploymentName))
        {
            var configSection = configuration.GetSection(configurationSectionName);
            deploymentName = configSection[DeploymentKey];
        }

        if (string.IsNullOrEmpty(deploymentName))
        {
            throw new InvalidOperationException($"An {nameof(IChatClient)} could not be configured. Ensure a '{DeploymentKey}' or '{ModelKey}' value is provided in 'ConnectionStrings:{connectionName}', or specify a '{DeploymentKey}' in the '{configurationSectionName}' configuration section, or specify a '{nameof(deploymentName)}' in the call.");
        }

        return deploymentName;
    }

    private static string? ConnectionStringValue(DbConnectionStringBuilder connectionString, string key)
        => connectionString.TryGetValue(key, out var value) ? value as string : null;
}
