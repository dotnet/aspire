// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    public static IResourceBuilder<ExternalServiceResource> AddExternalService(this IDistributedApplicationBuilder builder, [ResourceName] string name, string url)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(url);

        if (!ExternalServiceResource.UrlIsValidForExternalService(url, out _, out var message))
        {
            throw new ArgumentException($"The external service URL '{url}' is invalid: {message}", nameof(url));
        }

        var urlExpression = ReferenceExpression.Create($"{url}");
        return AddExternalServiceCore(builder, name, urlExpression);
    }

    /// <summary>
    /// Adds an external service resource to the distributed application with the specified URI.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="uri">The URI of the external service.</param>
    /// <returns>An <see cref="IResourceBuilder{ExternalServiceResource}"/> instance.</returns>
    public static IResourceBuilder<ExternalServiceResource> AddExternalService(this IDistributedApplicationBuilder builder, [ResourceName] string name, Uri uri)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(uri);

        if (!ExternalServiceResource.UrlIsValidForExternalService(uri.ToString(), out _, out var message))
        {
            throw new ArgumentException($"The external service URI '{uri}' is invalid: {message}", nameof(uri));
        }

        var urlExpression = ReferenceExpression.Create($"{uri.ToString()}");
        return AddExternalServiceCore(builder, name, urlExpression);
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

        return AddExternalServiceCore(builder, name, urlExpression);
    }

    /// <summary>
    /// Adds an external service resource to the distributed application where the URL is supplied via a parameter.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns>An <see cref="IResourceBuilder{ExternalServiceResource}"/> instance.</returns>
    /// <remarks>
    /// This overload automatically creates a parameter with the same name as the resource to supply the URL.
    /// </remarks>
    public static IResourceBuilder<ExternalServiceResource> AddExternalService(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        var parameter = builder.AddParameter(name);
        var urlExpression = ReferenceExpression.Create($"{parameter.Resource}");
        return AddExternalServiceCore(builder, name, urlExpression);
    }

    private static IResourceBuilder<ExternalServiceResource> AddExternalServiceCore(IDistributedApplicationBuilder builder, string name, ReferenceExpression urlExpression)
    {
        var resource = new ExternalServiceResource(name, urlExpression);
        return builder.AddResource(resource)
            .WithInitialState(new CustomResourceSnapshot
            {
                ResourceType = "ExternalService",
                Properties = []
            })
            .ExcludeFromManifest();
    }
}
