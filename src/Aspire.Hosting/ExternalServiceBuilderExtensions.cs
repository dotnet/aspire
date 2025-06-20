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

        var uri = new Uri(url);
        return builder.AddExternalService(name, uri);
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

        var rb = new ReferenceExpressionBuilder();
        rb.AppendLiteral(uri.ToString());
        var urlExpression = rb.Build();
        return builder.AddExternalService(name, urlExpression);
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

        var resource = new ExternalServiceResource(name, urlExpression);
        
        return builder.AddResource(resource)
                      .WithReferenceRelationship(urlExpression)
                      .WithInitialState(new CustomResourceSnapshot
                      {
                          ResourceType = "ExternalService",
                          Properties = [],
                          State = "Running"
                      })
                      .WithExternalServiceEndpoint(urlExpression);
    }

    /// <summary>
    /// Adds an endpoint annotation for an external service that represents the external URL.
    /// </summary>
    private static IResourceBuilder<ExternalServiceResource> WithExternalServiceEndpoint(this IResourceBuilder<ExternalServiceResource> builder, ReferenceExpression urlExpression)
    {
        // Create a default endpoint annotation
        builder.WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, "http", name: "default"));
        
        // Create an environment callback that simulates what ApplyEndpoints would do for external services
        builder.WithEnvironment((Action<EnvironmentCallbackContext>)(context =>
        {
            var serviceName = builder.Resource.Name;
            context.EnvironmentVariables[$"services__{serviceName}__default__0"] = urlExpression;
        }));

        return builder;
    }

    /// <summary>
    /// Adds an external service resource to the distributed application with a parameterized URL.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns>An <see cref="IResourceBuilder{ExternalServiceResource}"/> instance.</returns>
    public static IResourceBuilder<ExternalServiceResource> AddExternalService(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        // Create a parameter resource for the URL
        var parameter = builder.AddParameter($"{name}-url");
        var urlExpression = ReferenceExpression.Create($"{parameter}");
        
        return builder.AddExternalService(name, urlExpression);
    }
}