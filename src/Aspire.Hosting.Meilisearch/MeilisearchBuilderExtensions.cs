// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Meilisearch;
using Aspire.Hosting.Utils;
using Aspire.Meilisearch;
using Meilisearch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Meilisearch resources to the application model.
/// </summary>
public static class MeilisearchBuilderExtensions
{
    private const int MeilisearchPort = 7700;

    /// <summary>
    /// Adds an Meilisearch container resource to the application model.
    /// </summary>
    /// <remarks>
    /// The default image is "getmeili/meilisearch" and the tag is "v1.10".
    /// </remarks>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port to bind the underlying container to.</param>
    /// <param name="masterKey">The parameter used to provide the master key for the Meilisearch. If <see langword="null"/> a random master key will be generated.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <example>
    /// Add an Meilisearch container to the application model and reference it in a .NET project.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var meilisearch = builder.Meilisearch("meilisearch");
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api")
    ///   .WithReference(meilisearch);
    ///  
    /// builder.Build().Run(); 
    /// </code>
    /// </example>
    public static IResourceBuilder<MeilisearchResource> AddMeilisearch(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<ParameterResource>? masterKey = null,
        int? port = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        var masterKeyParameter = masterKey?.Resource ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-masterKey");

        var meilisearch = new MeilisearchResource(name, masterKeyParameter);

        MeilisearchClient? meilisearchClient = null;

        builder.Eventing.Subscribe<ConnectionStringAvailableEvent>(meilisearch, async (@event, ct) =>
        {
            var connectionString = await meilisearch.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false)
            ?? throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{meilisearch.Name}' resource but the connection string was null.");

            meilisearchClient = CreateMeilisearchClient(connectionString);
        });

        var healthCheckKey = $"{name}_check";
        builder.Services.AddHealthChecks()
         .Add(new HealthCheckRegistration(
             healthCheckKey,
             sp => new MeilisearchHealthCheck(meilisearchClient!),
             failureStatus: default,
             tags: default,
             timeout: default));

        return builder.AddResource(meilisearch)
             .WithImage(MeilisearchContainerImageTags.Image, MeilisearchContainerImageTags.Tag)
             .WithImageRegistry(MeilisearchContainerImageTags.Registry)
             .WithHttpEndpoint(targetPort: MeilisearchPort, port: port, name: MeilisearchResource.PrimaryEndpointName)
             .WithEnvironment(context =>
             {
                 context.EnvironmentVariables["MEILI_MASTER_KEY"] = meilisearch.MasterKeyParameter;
             })
             .WithHealthCheck(healthCheckKey);
    }

    /// <summary>
    /// Adds a named volume for the data folder to a Meilisearch container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    /// <example>
    /// Add an Meilisearch container to the application model and reference it in a .NET project. Additionally, in this
    /// example a data volume is added to the container to allow data to be persisted across container restarts.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var meilisearch = builder.AddMeilisearch("meilisearch")
    /// .WithDataVolume();
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api")
    ///   .WithReference(meilisearch);
    ///  
    /// builder.Build().Run(); 
    /// </code>
    /// </example>
    public static IResourceBuilder<MeilisearchResource> WithDataVolume(this IResourceBuilder<MeilisearchResource> builder, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithVolume(name ?? VolumeNameGenerator.CreateVolumeName(builder, "data"), "/meili_data");
    }

    /// <summary>
    /// Adds a bind mount for the data folder to a Meilisearch container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    /// <example>
    /// Add an Meilisearch container to the application model and reference it in a .NET project. Additionally, in this
    /// example a bind mount is added to the container to allow data to be persisted across container restarts.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var meilisearch = builder.AddMeilisearch("meilisearch")
    /// .WithDataBindMount("./data/meilisearch/data");
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api")
    ///   .WithReference(meilisearch);
    ///  
    /// builder.Build().Run(); 
    /// </code>
    /// </example>
    public static IResourceBuilder<MeilisearchResource> WithDataBindMount(this IResourceBuilder<MeilisearchResource> builder, string source)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        return builder.WithBindMount(source, "/meili_data");
    }

    internal static MeilisearchClient CreateMeilisearchClient(string? connectionString)
    {
        if (connectionString is null)
        {
            throw new InvalidOperationException("Connection string is unavailable");
        }

        Uri? endpoint = null;
        string? masterKey = null;

        if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
        {
            endpoint = uri;
        }
        else
        {
            var connectionBuilder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };

            if (connectionBuilder.TryGetValue("Endpoint", out var endpointValue) && Uri.TryCreate(endpointValue.ToString(), UriKind.Absolute, out var serviceUri))
            {
                endpoint = serviceUri;
            }

            if (connectionBuilder.TryGetValue("MasterKey", out var masterKeyValue))
            {
                masterKey = masterKeyValue.ToString();
            }
        }

        return new MeilisearchClient(endpoint!.ToString(), apiKey: masterKey!);
    }
}
