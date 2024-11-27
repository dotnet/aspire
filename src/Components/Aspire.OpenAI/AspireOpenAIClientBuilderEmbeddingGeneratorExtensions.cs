// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> as a singleton in the services provided by the <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class AspireOpenAIClientBuilderEmbeddingGeneratorExtensions
{
    /// <summary>
    /// Registers a singleton <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">An <see cref="AspireOpenAIClientBuilder" />.</param>
    /// <param name="deploymentName">Optionally specifies which model deployment to use. If not specified, a value will be taken from the connection string.</param>
    /// <returns>A <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> that can be used to build a pipeline around the inner <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>.</returns>
    public static EmbeddingGeneratorBuilder<string, Embedding<float>> AddEmbeddingGenerator(
        this AspireOpenAIClientBuilder builder,
        string? deploymentName = null)
    {
        return builder.HostBuilder.Services.AddEmbeddingGenerator(
            services => CreateInnerEmbeddingGenerator(services, builder, deploymentName));
    }

    /// <summary>
    /// Registers a keyed singleton <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">An <see cref="AspireOpenAIClientBuilder" />.</param>
    /// <param name="serviceKey">The service key with which the <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> will be registered.</param>
    /// <param name="deploymentName">Optionally specifies which model deployment to use. If not specified, a value will be taken from the connection string.</param>
    /// <returns>A <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> that can be used to build a pipeline around the inner <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>.</returns>
    public static EmbeddingGeneratorBuilder<string, Embedding<float>> AddKeyedEmbeddingGenerator(
        this AspireOpenAIClientBuilder builder,
        string serviceKey,
        string? deploymentName = null)
    {
        return builder.HostBuilder.Services.AddKeyedEmbeddingGenerator(
            serviceKey,
            services => CreateInnerEmbeddingGenerator(services, builder, deploymentName));
    }

    private static IEmbeddingGenerator<string, Embedding<float>> CreateInnerEmbeddingGenerator(
        IServiceProvider services,
        AspireOpenAIClientBuilder builder,
        string? deploymentName)
    {
        var openAiClient = builder.ServiceKey is null
            ? services.GetRequiredService<OpenAIClient>()
            : services.GetRequiredKeyedService<OpenAIClient>(builder.ServiceKey);

        deploymentName ??= builder.GetRequiredDeploymentName();
        var result = openAiClient.AsEmbeddingGenerator(deploymentName);

        return builder.DisableTracing
            ? result
            : new OpenTelemetryEmbeddingGenerator<string, Embedding<float>>(result);
    }
}
