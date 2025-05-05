// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Azure;
using Azure.AI.Inference;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for adding Azure AI Inference services to an Aspire application.
/// </summary>
public static class AspireAzureAIInferenceExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Azure:AI:Inference";

    /// <summary>
    /// Adds a <see cref="ChatCompletionsClient"/> to the application and configures it with the specified settings.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to add the client to.</param>
    /// <param name="connectionName">The name of the client. This is used to retrieve the connection string from configuration.</param>
    /// <param name="configureClient">An optional callback to configure the <see cref="ChatCompletionsClientSettings"/>.</param>
    /// <param name="configureClientBuilder">An optional callback to configure the <see cref="IAzureClientBuilder{TClient, TOptions}"/> for the client.</param>
    /// <returns>An <see cref="AspireChatCompletionsClientBuilder"/> that can be used to further configure the client.</returns>
    /// <exception cref="InvalidOperationException">Thrown when endpoint is missing from settings.</exception>
    /// <remarks>
    /// <para>
    /// The client is registered as a singleton with a keyed service.
    /// </para>
    /// <para>
    /// Configuration is loaded from the "Aspire:Azure:AI:Inference" section, and can be supplemented with a connection string named after the <paramref name="connectionName"/> parameter.
    /// </para>
    /// </remarks>
    public static AspireChatCompletionsClientBuilder AddChatCompletionsClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<ChatCompletionsClientSettings>? configureClient = null,
        Action<IAzureClientBuilder<ChatCompletionsClient, AzureAIInferenceClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        var settings = new ChatCompletionsClientServiceComponent().AddClient(
            builder,
            DefaultConfigSectionName,
            configureClient,
            configureClientBuilder,
            connectionName,
            serviceKey: null);

        return new AspireChatCompletionsClientBuilder(builder, serviceKey: null, settings.ModelId, settings.DisableTracing);
    }

    /// <summary>
    /// Adds a <see cref="ChatCompletionsClient"/> to the application and configures it with the specified settings.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to add the client to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureClient">An optional callback to configure the <see cref="ChatCompletionsClientSettings"/>.</param>
    /// <param name="configureClientBuilder">An optional callback to configure the <see cref="IAzureClientBuilder{TClient, TOptions}"/> for the client.</param>
    /// <returns>An <see cref="AspireChatCompletionsClientBuilder"/> that can be used to further configure the client.</returns>
    /// <exception cref="InvalidOperationException">Thrown when endpoint is missing from settings.</exception>
    /// <remarks>
    /// <para>
    /// The client is registered as a singleton with a keyed service.
    /// </para>
    /// <para>
    /// Configuration is loaded from the "Aspire:Azure:AI:Inference" section, and can be supplemented with a connection string named after the <paramref name="name"/> parameter.
    /// </para>
    /// </remarks>
    public static AspireChatCompletionsClientBuilder AddKeyedChatCompletionsClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<ChatCompletionsClientSettings>? configureClient = null,
        Action<IAzureClientBuilder<ChatCompletionsClient, AzureAIInferenceClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var settings = new ChatCompletionsClientServiceComponent().AddClient(
            builder,
            DefaultConfigSectionName,
            configureClient,
            configureClientBuilder,
            name,
            serviceKey: name);

        return new AspireChatCompletionsClientBuilder(builder, serviceKey: name, settings.ModelId, settings.DisableTracing);
    }

    private sealed class ChatCompletionsClientServiceComponent : AzureComponent<ChatCompletionsClientSettings, ChatCompletionsClient, AzureAIInferenceClientOptions>
    {
        protected override IAzureClientBuilder<ChatCompletionsClient, AzureAIInferenceClientOptions> AddClient(
            AzureClientFactoryBuilder azureFactoryBuilder,
            ChatCompletionsClientSettings settings,
            string connectionName, string
            configurationSectionName)
        {
            return azureFactoryBuilder.AddClient<ChatCompletionsClient, AzureAIInferenceClientOptions>((options, _, _) =>
            {
                if (settings.Endpoint is null)
                {
                    throw new InvalidOperationException($"An ChatCompletionsClient could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or specify a '{nameof(ChatCompletionsClientSettings.Endpoint)}' or '{nameof(ChatCompletionsClientSettings.Key)}' in the '{configurationSectionName}' configuration section.");
                }
                else
                {
                    // Connect to Azure AI Foundry using key auth
                    if (!string.IsNullOrEmpty(settings.Key))
                    {
                        var credential = new AzureKeyCredential(settings.Key);
                        return new ChatCompletionsClient(settings.Endpoint, credential, options);
                    }
                    else
                    {
                        return new ChatCompletionsClient(settings.Endpoint, settings.TokenCredential ?? new DefaultAzureCredential(), options);
                    }
                }
            });
        }

        protected override void BindClientOptionsToConfiguration(IAzureClientBuilder<ChatCompletionsClient, AzureAIInferenceClientOptions> clientBuilder, IConfiguration configuration)
        {
#pragma warning disable IDE0200 // Remove unnecessary lambda expression - needed so the ConfigBinder Source Generator works
            clientBuilder.ConfigureOptions(options => configuration.Bind(options));
#pragma warning restore IDE0200
        }

        protected override void BindSettingsToConfiguration(ChatCompletionsClientSettings settings, IConfiguration configuration)
            => configuration.Bind(settings);

        protected override IHealthCheck CreateHealthCheck(ChatCompletionsClient client, ChatCompletionsClientSettings settings)
            => throw new NotImplementedException();

        protected override bool GetHealthCheckEnabled(ChatCompletionsClientSettings settings)
            => false;

        protected override bool GetMetricsEnabled(ChatCompletionsClientSettings settings)
            => !settings.DisableMetrics;

        protected override TokenCredential? GetTokenCredential(ChatCompletionsClientSettings settings)
            => settings.TokenCredential;

        protected override bool GetTracingEnabled(ChatCompletionsClientSettings settings)
            => !settings.DisableTracing;
    }

    /// <summary>
    /// Creates a <see cref="IChatClient"/> from the <see cref="ChatCompletionsClient"/> registered in the service collection.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static ChatClientBuilder AddChatClient(this AspireChatCompletionsClientBuilder builder) =>
        builder.Builder.Services.AddChatClient(services =>
        {
            var chatCompletionsClient = !string.IsNullOrEmpty(builder.ServiceKey) ?
                services.GetRequiredService<ChatCompletionsClient>() :
                services.GetRequiredKeyedService<ChatCompletionsClient>(builder.ServiceKey);

            var result = chatCompletionsClient.AsIChatClient();

            if (builder.DisableTracing)
            {
                return result;
            }

            var loggerFactory = services.GetService<ILoggerFactory>();
            return new OpenTelemetryChatClient(result, loggerFactory?.CreateLogger(typeof(OpenTelemetryChatClient)));
        });
}
