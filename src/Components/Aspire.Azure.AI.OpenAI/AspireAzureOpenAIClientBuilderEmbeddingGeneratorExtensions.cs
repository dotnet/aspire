// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> as a singleton in the services provided by the <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class AspireAzureOpenAIClientBuilderEmbeddingGeneratorExtensions
{
    private const string DeploymentKey = "Deployment";
    private const string ModelKey = "Model";

    /// <summary>
    /// Registers a singleton <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">An <see cref="AspireAzureOpenAIClientBuilder" />.</param>
    /// <param name="deploymentName">Optionally specifies which model deployment to use. If not specified, a value will be taken from the connection string.</param>
    /// <param name="disableOpenTelemetry">Optional. If <see langword="true"/>, skips registering open telemetry support in the <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> pipeline.</param>
    /// <returns>A <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> that can be used to build a pipeline around the inner <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>.</returns>
    /// <remarks>Reads the configuration from "Aspire.Azure.AI.OpenAI" section.</remarks>
    public static EmbeddingGeneratorBuilder<string, Embedding<float>> AddEmbeddingGenerator(
        this AspireAzureOpenAIClientBuilder builder,
        string? deploymentName = null,
        bool disableOpenTelemetry = false)
    {
        return builder.HostBuilder.Services.AddEmbeddingGenerator(
            services => CreateInnerEmbeddingGenerator(services, builder, deploymentName, disableOpenTelemetry));
    }

    /// <summary>
    /// Registers a keyed singleton <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">An <see cref="AspireAzureOpenAIClientBuilder" />.</param>
    /// <param name="serviceKey">The service key with which the <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> will be registered.</param>
    /// <param name="deploymentName">Optionally specifies which model deployment to use. If not specified, a value will be taken from the connection string.</param>
    /// <param name="disableOpenTelemetry">Optional. If <see langword="true"/>, skips registering open telemetry support in the <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> pipeline.</param>
    /// <returns>A <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> that can be used to build a pipeline around the inner <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>.</returns>
    /// <remarks>Reads the configuration from "Aspire.Azure.AI.OpenAI" section.</remarks>
    public static EmbeddingGeneratorBuilder<string, Embedding<float>> AddKeyedEmbeddingGenerator(
        this AspireAzureOpenAIClientBuilder builder,
        string serviceKey,
        string? deploymentName = null,
        bool disableOpenTelemetry = false)
    {
        return builder.HostBuilder.Services.AddKeyedEmbeddingGenerator(
            serviceKey,
            services => CreateInnerEmbeddingGenerator(services, builder, deploymentName, disableOpenTelemetry));
    }

    private static IEmbeddingGenerator<string, Embedding<float>> CreateInnerEmbeddingGenerator(
        IServiceProvider services,
        AspireAzureOpenAIClientBuilder builder,
        string? deploymentName,
        bool disableOpenTelemetry)
    {
        var openAiClient = builder.ServiceKey is null
            ? services.GetRequiredService<AzureOpenAIClient>()
            : services.GetRequiredKeyedService<AzureOpenAIClient>(builder.ServiceKey);

        deploymentName ??= GetRequiredDeploymentName(builder.HostBuilder.Configuration, builder.ConnectionName);
        var result = openAiClient.AsEmbeddingGenerator(deploymentName);

        return disableOpenTelemetry
            ? result
            : new OpenTelemetryEmbeddingGenerator<string, Embedding<float>>(result);
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
            throw new InvalidOperationException($"An {nameof(IEmbeddingGenerator<string, Embedding<float>>)} could not be configured. Ensure a '{DeploymentKey}' or '{ModelKey}' value is provided in 'ConnectionStrings:{connectionName}', or specify a '{DeploymentKey}' in the '{configurationSectionName}' configuration section, or specify a '{nameof(deploymentName)}' in the call.");
        }

        return deploymentName;
    }

    private static string? ConnectionStringValue(DbConnectionStringBuilder connectionString, string key)
        => connectionString.TryGetValue(key, out var value) ? value as string : null;
}
