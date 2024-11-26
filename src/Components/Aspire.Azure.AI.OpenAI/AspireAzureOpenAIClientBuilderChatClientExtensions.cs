// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering <see cref="IChatClient"/> as a singleton in the services provided by the <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class AspireAzureOpenAIClientBuilderChatClientExtensions
{
    private const string DeploymentKey = "Deployment";
    private const string ModelKey = "Model";

    /// <summary>
    /// Registers a singleton <see cref="IChatClient"/> in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">An <see cref="AspireAzureOpenAIClientBuilder" />.</param>
    /// <param name="deploymentName">Optionally specifies which model deployment to use. If not specified, a value will be taken from the connection string.</param>
    /// <param name="configurePipeline">An optional method that can be used for customizing the <see cref="IChatClient"/> pipeline.</param>
    /// <remarks>Reads the configuration from "Aspire.Azure.AI.OpenAI" section.</remarks>
    public static void AddChatClient(
        this AspireAzureOpenAIClientBuilder builder,
        string? deploymentName = null,
        Func<ChatClientBuilder, ChatClientBuilder>? configurePipeline = null)
    {
        builder.HostBuilder.Services.AddSingleton(
            services => CreateChatClient(services, builder, deploymentName, configurePipeline));
    }

    /// <summary>
    /// Registers a keyed singleton <see cref="IChatClient"/> in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">An <see cref="AspireAzureOpenAIClientBuilder" />.</param>
    /// <param name="serviceKey">The service key with which the <see cref="IChatClient"/> will be registered.</param>
    /// <param name="deploymentName">Optionally specifies which model deployment to use. If not specified, a value will be taken from the connection string.</param>
    /// <param name="configurePipeline">An optional method that can be used for customizing the <see cref="IChatClient"/> pipeline.</param>
    /// <remarks>Reads the configuration from "Aspire.Azure.AI.OpenAI" section.</remarks>
    public static void AddKeyedChatClient(
        this AspireAzureOpenAIClientBuilder builder,
        string serviceKey,
        string? deploymentName = null,
        Func<ChatClientBuilder, ChatClientBuilder>? configurePipeline = null)
    {
        builder.HostBuilder.Services.TryAddKeyedSingleton(
            serviceKey,
            (services, _) => CreateChatClient(services, builder, deploymentName, configurePipeline));
    }

    private static IChatClient CreateChatClient(
        IServiceProvider services,
        AspireAzureOpenAIClientBuilder builder,
        string? deploymentName,
        Func<ChatClientBuilder, ChatClientBuilder>? configurePipeline)
    {
        var openAiClient = builder.ServiceKey is null
            ? services.GetRequiredService<AzureOpenAIClient>()
            : services.GetRequiredKeyedService<AzureOpenAIClient>(builder.ServiceKey);

        deploymentName ??= GetRequiredDeploymentName(builder.HostBuilder.Configuration, builder.ConnectionName);
        var chatClientBuilder = new ChatClientBuilder(openAiClient.AsChatClient(deploymentName));
        configurePipeline?.Invoke(chatClientBuilder);

        return chatClientBuilder.Build(services);
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

        var configurationSectionName = AspireAzureOpenAIExtensions.DefaultConfigSectionName;
        if (string.IsNullOrEmpty(deploymentName))
        {
            var configSection = configuration.GetSection(configurationSectionName);
            deploymentName = configSection[DeploymentKey];
        }

        if (string.IsNullOrEmpty(deploymentName))
        {
            throw new InvalidOperationException($"An {nameof(IChatClient)} could not be configured. Ensure a '{DeploymentKey}' or '{ModelKey}' value is provided in 'ConnectionStrings:{connectionName}', or specify a '{DeploymentKey}' in the '{configurationSectionName}' configuration section, or specify a '{nameof(deploymentName)}' in the call to {nameof(AddChatClient)}.");
        }

        return deploymentName;
    }

    private static string? ConnectionStringValue(DbConnectionStringBuilder connectionString, string key)
        => connectionString.TryGetValue(key, out var value) ? value as string : null;
}
