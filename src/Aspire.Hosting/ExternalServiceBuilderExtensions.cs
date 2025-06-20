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
                          State = "Starting"
                      })
                      .WithExternalServiceEndpoints()
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
    private static IResourceBuilder<ExternalServiceResource> WithExternalServiceEndpoints(this IResourceBuilder<ExternalServiceResource> builder)
    {
        var urlExpression = builder.Resource.UrlExpression;
        
        // Determine the scheme from the URL if it's a literal URL
        var scheme = "http"; // default
        if (IsLiteralUrl(urlExpression, out var literalUrl) && Uri.TryCreate(literalUrl, UriKind.Absolute, out var uri))
        {
            scheme = uri.Scheme;
        }
        
        // Add a default endpoint annotation that represents the external service
        // This allows GetEndpoint() to work and enables endpoint references
        var endpointAnnotation = new EndpointAnnotation(ProtocolType.Tcp, scheme);
        
        // Create a special allocated endpoint that will resolve the URL expression at runtime
        endpointAnnotation.AllocatedEndpoint = new ExternalServiceAllocatedEndpoint(endpointAnnotation, urlExpression);
        
        builder.WithAnnotation(endpointAnnotation);

        return builder;
    }

    /// <summary>
    /// Checks if a ReferenceExpression represents a literal URL string.
    /// </summary>
    private static bool IsLiteralUrl(ReferenceExpression expression, out string url)
    {
        // If the expression has no value providers, it's a literal string
        if (expression.ValueProviders.Count == 0)
        {
            url = expression.Format;
            return true;
        }

        url = string.Empty;
        return false;
    }
}