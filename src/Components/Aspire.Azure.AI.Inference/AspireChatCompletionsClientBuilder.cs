// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.AI.Inference;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides a builder for configuring and integrating an Aspire Chat Completions client into a host application.
/// </summary>
/// <remarks>This class is used to configure the necessary parameters for creating an Aspire Chat Completions
/// client, such as the host application builder, service key, and optional model ID. It is intended for internal use
/// within the application setup process.</remarks>
/// <param name="hostBuilder">The <see cref="IHostApplicationBuilder"/> with which services are being registered.</param>
/// <param name="serviceKey">The service key used to register the <see cref="ChatCompletionsClient"/> service, if any.</param>
/// <param name="deploymentId">The id of the deployment in Azure AI Foundry.</param>
/// <param name="disableTracing">A flag to indicate whether tracing should be disabled.</param>
public class AspireChatCompletionsClientBuilder(
    IHostApplicationBuilder hostBuilder,
    string? serviceKey,
    string? deploymentId,
    bool disableTracing)
{
    /// <summary>
    /// Gets a flag indicating whether tracing should be disabled.
    /// </summary>
    public bool DisableTracing { get; } = disableTracing;

    /// <summary>
    /// Gets the <see cref="IHostApplicationBuilder"/> with which services are being registered.
    /// </summary>
    public IHostApplicationBuilder HostBuilder { get; } = hostBuilder ?? throw new ArgumentNullException(nameof(hostBuilder));

    /// <summary>
    /// Gets the service key used to register the <see cref="ChatCompletionsClient"/> service, if any.
    /// </summary>
    public string? ServiceKey { get; } = serviceKey;

    /// <summary>
    /// The ID of the deployment in Azure AI Foundry.
    /// </summary>
    public string? DeploymentId { get; } = deploymentId;
}
