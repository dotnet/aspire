// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.AI.OpenAI;
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
    /// <summary>
    /// Registers a singleton <see cref="IChatClient"/> in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">An <see cref="AspireAzureOpenAIClientBuilder" />.</param>
    /// <param name="deploymentName">Optionally specifies which model deployment to use. If not specified, a value will be taken from the connection string.</param>
    /// <param name="configurePipeline">An optional method that can be used for customizing the <see cref="IChatClient"/> pipeline.</param>
    /// <remarks>Reads the configuration from "Aspire.Azure.AI.OpenAI" section.</remarks>
    public static AspireAzureOpenAIClientBuilder AddChatClient(
        this AspireAzureOpenAIClientBuilder builder,
        string? deploymentName = null,
        Func<ChatClientBuilder, ChatClientBuilder>? configurePipeline = null)
    {
        builder.HostBuilder.Services.AddSingleton(
            services => CreateChatClient(services, builder, deploymentName, configurePipeline));

        return builder;
    }

    /// <summary>
    /// Registers a keyed singleton <see cref="IChatClient"/> in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">An <see cref="AspireAzureOpenAIClientBuilder" />.</param>
    /// <param name="serviceKey">The service key with which the <see cref="IChatClient"/> will be registered.</param>
    /// <param name="configurePipeline">An optional method that can be used for customizing the <see cref="IChatClient"/> pipeline.</param>
    /// <remarks>Reads the configuration from "Aspire.Azure.AI.OpenAI" section.</remarks>
    public static AspireAzureOpenAIClientBuilder AddKeyedChatClient(
        this AspireAzureOpenAIClientBuilder builder,
        string serviceKey,
        Func<ChatClientBuilder, ChatClientBuilder>? configurePipeline = null)
    {
        builder.HostBuilder.Services.TryAddKeyedSingleton(
            serviceKey,
            (services, _) => CreateChatClient(services, builder, serviceKey, configurePipeline));

        return builder;
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

        var chatClientBuilder = new ChatClientBuilder(services);
        configurePipeline?.Invoke(chatClientBuilder);

        var deploymentSettings = GetDeployments(builder.HostBuilder.Configuration, builder.ConnectionName);

        // If no deployment name is provided, we search for the first one (and maybe only one) in configuration
        if (deploymentName is null)
        {
            deploymentName = deploymentSettings.Models.Keys.FirstOrDefault();

            if (string.IsNullOrEmpty(deploymentName))
            {
                throw new InvalidOperationException($"An {nameof(IChatClient)} could not be configured. Ensure a deployment was defined .");
            }
        }

        if (!deploymentSettings.Models.TryGetValue(deploymentName, out var _))
        {
            throw new InvalidOperationException($"An {nameof(IChatClient)} could not be configured. Ensure the deployment name '{deploymentName}' was defined .");
        }

        return chatClientBuilder.Use(openAiClient.AsChatClient(deploymentName));
    }

    private static DeploymentModelSettings GetDeployments(IConfiguration configuration, string connectionName)
    {
        var configurationSectionName = $"{AspireAzureOpenAIExtensions.DefaultConfigSectionName}:{connectionName}";
        var configSection = configuration.GetSection(configurationSectionName);

        var settings = new DeploymentModelSettings();
        configSection.Bind(settings);

        return settings;
    }
}
