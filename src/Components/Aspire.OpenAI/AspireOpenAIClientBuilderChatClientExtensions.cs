// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering <see cref="IChatClient"/> as a singleton in the services provided by the <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class AspireOpenAIClientBuilderChatClientExtensions
{
    /// <summary>
    /// Registers a singleton <see cref="IChatClient"/> in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">An <see cref="AspireOpenAIClientBuilder" />.</param>
    /// <param name="deploymentName">Optionally specifies which model deployment to use. If not specified, a value will be taken from the connection string.</param>
    /// <returns>A <see cref="ChatClientBuilder"/> that can be used to build a pipeline around the inner <see cref="IChatClient"/>.</returns>
    public static ChatClientBuilder AddChatClient(
        this AspireOpenAIClientBuilder builder,
        string? deploymentName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.HostBuilder.Services.AddChatClient(
            services => CreateInnerChatClient(services, builder, deploymentName));
    }

    /// <summary>
    /// Registers a keyed singleton <see cref="IChatClient"/> in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">An <see cref="AspireOpenAIClientBuilder" />.</param>
    /// <param name="serviceKey">The service key with which the <see cref="IChatClient"/> will be registered.</param>
    /// <param name="deploymentName">Optionally specifies which model deployment to use. If not specified, a value will be taken from the connection string.</param>
    /// <returns>A <see cref="ChatClientBuilder"/> that can be used to build a pipeline around the inner <see cref="IChatClient"/>.</returns>
    public static ChatClientBuilder AddKeyedChatClient(
        this AspireOpenAIClientBuilder builder,
        string serviceKey,
        string? deploymentName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(serviceKey);

        return builder.HostBuilder.Services.AddKeyedChatClient(
            serviceKey,
            services => CreateInnerChatClient(services, builder, deploymentName));
    }

    private static IChatClient CreateInnerChatClient(
        IServiceProvider services,
        AspireOpenAIClientBuilder builder,
        string? deploymentName)
    {
        var openAiClient = builder.ServiceKey is null
            ? services.GetRequiredService<OpenAIClient>()
            : services.GetRequiredKeyedService<OpenAIClient>(builder.ServiceKey);

        deploymentName ??= builder.GetRequiredDeploymentName();
        var result = openAiClient.AsChatClient(deploymentName);

        return builder.DisableTracing ? result : new OpenTelemetryChatClient(result);
    }
}
