// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using Aspire.Azure.AI.OpenAI;
using Azure.AI.OpenAI;
using Azure.Core.Extensions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering <see cref="IChatClient"/> as a singleton in the services provided by the <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class AspireAzureOpenAIChatClientExtensions
{
    private const string DeployentKey = "Deployment";
    private const string ModelKey = "Model";

    /// <summary>
    /// Registers a singleton <see cref="IChatClient"/> in the services provided by the <paramref name="builder"/>.
    ///
    /// Additionally, registers the underlying <see cref="AzureOpenAIClient"/> and <see cref="OpenAIClient"/> as singleton services.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configurePipeline">An optional method that can be used for customizing the <see cref="IChatClient"/> pipeline.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureOpenAISettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{AzureOpenAIClient, AzureOpenAIClientOptions}"/>.</param>
    /// <param name="deploymentName">Optionally specifies the deployment name. If not specified, a value will be taken from the connection string.</param>
    /// <remarks>Reads the configuration from "Aspire.Azure.AI.OpenAI" section.</remarks>
    public static void AddAzureOpenAIChatClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Func<ChatClientBuilder, ChatClientBuilder>? configurePipeline = null,
        Action<AzureOpenAISettings>? configureSettings = null,
        Action<IAzureClientBuilder<AzureOpenAIClient, AzureOpenAIClientOptions>>? configureClientBuilder = null,
        string? deploymentName = null)
    {
        builder.AddAzureOpenAIClient(connectionName, configureSettings, configureClientBuilder);

        builder.Services.AddSingleton(services =>
        {
            var chatClientBuilder = new ChatClientBuilder(services);
            configurePipeline?.Invoke(chatClientBuilder);

            deploymentName ??= GetRequiredDeploymentName(builder.Configuration, connectionName);

            var innerClient = chatClientBuilder.Services
                .GetRequiredService<AzureOpenAIClient>()
                .AsChatClient(deploymentName);

            return chatClientBuilder.Use(innerClient);
        });
    }

    private static string GetRequiredDeploymentName(IConfiguration configuration, string connectionName)
    {
        string? deploymentName = null;

        if (configuration.GetConnectionString(connectionName) is string connectionString)
        {
            var connectionBuilder = new DbConnectionStringBuilder { ConnectionString = connectionString };
            deploymentName = (connectionBuilder[DeployentKey] ?? connectionBuilder[ModelKey]).ToString();
        }

        var configurationSectionName = AspireAzureOpenAIExtensions.DefaultConfigSectionName;
        if (string.IsNullOrEmpty(deploymentName))
        {
            var configSection = configuration.GetSection(configurationSectionName);
            deploymentName = configSection[DeployentKey];
        }

        if (string.IsNullOrEmpty(deploymentName))
        {
            throw new InvalidOperationException($"An {nameof(IChatClient)} could not be configured. Ensure a '{DeployentKey}' or '{ModelKey}' value is provided in 'ConnectionStrings:{connectionName}', or specify a '{DeployentKey}' in the '{configurationSectionName}' configuration section, or specify a '{nameof(deploymentName)}' in the call to {nameof(AddAzureOpenAIChatClient)}.");
        }

        return deploymentName;
    }
}
