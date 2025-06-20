// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding external services to an application.
/// </summary>
public static class ExternalServiceBuilderExtensions
{
    /// <summary>
    /// Adds an external service resource to the distributed application with the specified URL.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="url">The URL of the external service.</param>
    /// <returns>An <see cref="IResourceBuilder{ExternalServiceResource}"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the URL is not a valid, absolute URI.</exception>
    public static IResourceBuilder<ExternalServiceResource> AddExternalService(this IDistributedApplicationBuilder builder, [ResourceName] string name, string url)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(url);

        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            throw new ArgumentException($"The URL '{url}' is not a valid absolute URI.", nameof(url));
        }

        return AddExternalServiceCore(builder, name, url);
    }

    /// <summary>
    /// Adds an external service resource to the distributed application with the specified URI.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="uri">The URI of the external service.</param>
    /// <returns>An <see cref="IResourceBuilder{ExternalServiceResource}"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the URI is not absolute.</exception>
    public static IResourceBuilder<ExternalServiceResource> AddExternalService(this IDistributedApplicationBuilder builder, [ResourceName] string name, Uri uri)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(uri);

        if (!uri.IsAbsoluteUri)
        {
            throw new ArgumentException("The URI for the external service must be absolute.", nameof(uri));
        }

        return AddExternalServiceCore(builder, name, uri.ToString());
    }

    /// <summary>
    /// Adds an external service resource to the distributed application with the specified URL expression.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="urlExpression">The URL expression for the external service.</param>
    /// <returns>An <see cref="IResourceBuilder{ExternalServiceResource}"/> instance.</returns>
    public static IResourceBuilder<ExternalServiceResource> AddExternalService(this IDistributedApplicationBuilder builder, [ResourceName] string name, ReferenceExpression urlExpression)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(urlExpression);

        // For expressions, we'll store the expression format as the URL
        // The actual URL will be resolved at runtime through the expression
        var url = urlExpression.ValueExpression;
        var resource = new ExternalServiceResource(name, url);
        
        return builder.AddResource(resource)
                      .WithReferenceRelationship(urlExpression)
                      .WithInitialState(new CustomResourceSnapshot
                      {
                          ResourceType = "ExternalService",
                          Properties = [],
                          State = "Starting"
                      })
                      .WithExternalServiceEndpoints(urlExpression)
                      .WithHttpHealthCheck();
    }

    /// <summary>
    /// Adds an external service resource to the distributed application with a parameterized URL.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns>An <see cref="IResourceBuilder{IResourceWithServiceDiscovery}"/> instance.</returns>
    public static IResourceBuilder<IResourceWithServiceDiscovery> AddExternalService(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        return builder.AddParameter(
            new ExternalServiceParameterResource(
                name,
                parameterDefault => GetParameterValue(builder.Configuration, name, parameterDefault)));
    }

    private static IResourceBuilder<ExternalServiceResource> AddExternalServiceCore(IDistributedApplicationBuilder builder, string name, string url)
    {
        var resource = new ExternalServiceResource(name, url);
        
        return builder.AddResource(resource)
                      .WithInitialState(new CustomResourceSnapshot
                      {
                          ResourceType = "ExternalService",
                          Properties = [],
                          State = "Starting"
                      })
                      .WithExternalServiceEndpoints()
                      .WithHttpHealthCheck();
    }

    private static string GetParameterValue(Microsoft.Extensions.Configuration.ConfigurationManager configuration, string name, ParameterDefault? parameterDefault)
    {
        var configurationKey = $"Parameters:{name}";
        return configuration[configurationKey]
            ?? parameterDefault?.GetDefaultValue()
            ?? throw new DistributedApplicationException($"External service parameter resource could not be used because parameter '{name}' is missing.");
    }

    /// <summary>
    /// Configures the external service to provide endpoints for service discovery and endpoint references.
    /// </summary>
    private static IResourceBuilder<ExternalServiceResource> WithExternalServiceEndpoints(this IResourceBuilder<ExternalServiceResource> builder, ReferenceExpression? urlExpression = null)
    {
        var resource = builder.Resource;
        var url = resource.Url;
        
        // Determine the scheme from the URL
        var scheme = "http"; // default
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            scheme = uri.Scheme;
        }
        
        // Add a default endpoint annotation that represents the external service
        // This allows GetEndpoint() to work and enables endpoint references
        var endpointAnnotation = new EndpointAnnotation(ProtocolType.Tcp, uriScheme: scheme, isProxied: false);
        
        // Create the allocated endpoint immediately from the URL
        AllocatedEndpoint allocatedEndpoint;
        
        if (urlExpression != null)
        {
            // For parameterized URLs, use placeholder values that will be resolved at runtime
            var placeholderHost = "external.service";
            var defaultPort = scheme.ToLowerInvariant() switch
            {
                "https" => 443,
                "http" => 80,
                "ftp" => 21,
                "ws" => 80,
                "wss" => 443,
                _ => 80
            };
            allocatedEndpoint = new AllocatedEndpoint(endpointAnnotation, placeholderHost, defaultPort);
        }
        else if (Uri.TryCreate(url, UriKind.Absolute, out var serviceUri))
        {
            // For literal URLs, extract the actual host and port
            var host = serviceUri.Host;
            var port = serviceUri.Port;
            allocatedEndpoint = new AllocatedEndpoint(endpointAnnotation, host, port);
        }
        else
        {
            // Fallback for invalid URLs
            allocatedEndpoint = new AllocatedEndpoint(endpointAnnotation, "external.service", 80);
        }
        
        // Assign the allocated endpoint immediately
        endpointAnnotation.AllocatedEndpoint = allocatedEndpoint;
        
        builder.WithAnnotation(endpointAnnotation);

        // Subscribe to the InitializeResourceEvent to publish the ResourceEndpointsAllocatedEvent when the resource is initialized
        builder.ApplicationBuilder.Eventing.Subscribe<InitializeResourceEvent>(resource, async (e, ct) =>
        {
            if (e.Resource == resource)
            {
                // Publish the ResourceEndpointsAllocatedEvent to indicate endpoints have been allocated
                await e.Eventing.PublishAsync(new ResourceEndpointsAllocatedEvent(resource, e.Services), ct).ConfigureAwait(false);
            }
        });

        return builder;
    }
}