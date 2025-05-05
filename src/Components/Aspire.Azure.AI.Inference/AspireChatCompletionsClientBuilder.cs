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
/// <param name="builder">The <see cref="IHostApplicationBuilder"/> with which services are being registered.</param>
/// <param name="serviceKey">The service key used to register the <see cref="ChatCompletionsClient"/> service, if any.</param>
/// <param name="modelId">The name of the model (deployment) in Azure AI Foundry.</param>
/// <param name="disableTracing">A flag to indicate whether tracing should be disabled.</param>
public class AspireChatCompletionsClientBuilder(
    IHostApplicationBuilder builder,
    string? serviceKey,
    string? modelId,
    bool disableTracing)
{
    internal bool DisableTracing { get; } = disableTracing;
    internal IHostApplicationBuilder Builder { get; } = builder;
    internal string? ServiceKey { get; } = serviceKey;
    internal string? ModelId { get; } = modelId;
}
