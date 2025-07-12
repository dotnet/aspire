// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

        if (!ExternalServiceResource.UrlIsValidForExternalService(url, out var uri, out var message))
        {
            throw new ArgumentException($"The external service URL '{url}' is invalid: {message}", nameof(url));
        }

        return AddExternalServiceImpl(builder, name, uri);
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

        return AddExternalServiceImpl(builder, name, uri);
    }

    /// <summary>
    /// Adds an external service resource to the distributed application with the URL coming from the specified parameter.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="urlParameter">The parameter containing the URL of the external service.</param>
    /// <returns>An <see cref="IResourceBuilder{ExternalServiceResource}"/> instance.</returns>
    public static IResourceBuilder<ExternalServiceResource> AddExternalService(this IDistributedApplicationBuilder builder, [ResourceName] string name, IResourceBuilder<ParameterResource> urlParameter)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(urlParameter);

        return AddExternalServiceImpl(builder, name, urlParameter: urlParameter.Resource);
    }

    private static IResourceBuilder<ExternalServiceResource> AddExternalServiceImpl(IDistributedApplicationBuilder builder, string name, Uri? uri = null, ParameterResource? urlParameter = null)
    {
        Debug.Assert(uri is not null || urlParameter is not null, "Either uri or urlParameter must be provided.");

        var resource = uri is not null
            ? new ExternalServiceResource(name, uri)
            : new ExternalServiceResource(name, urlParameter!);

        var resourceBuilder = builder.AddResource(resource)
            .WithInitialState(new CustomResourceSnapshot
            {
                ResourceType = "ExternalService",
                State = KnownResourceStates.Waiting,
                Properties = []
            })
            .ExcludeFromManifest();

        if (resource.Uri is not null)
        {
            resourceBuilder.WithUrl(resource.Uri.ToString());
        }
        else if (resource.UrlParameter is not null)
        {
            resourceBuilder.WithUrl(ReferenceExpression.Create($"{resource.UrlParameter}"));
        }

        // Subscribe to the InitializeResourceEvent to finish setting up the resource
        builder.Eventing.Subscribe<InitializeResourceEvent>(resource, static async (e, ct) =>
        {
            var resource = e.Resource as ExternalServiceResource;
            if (resource is not null)
            {
                var uri = resource.Uri;

                if (uri is null)
                {
                    // If the URI is not set, it means we are using a parameterized URL
                    var url = resource.UrlParameter is null
                            ? null
                            : await resource.UrlParameter.GetValueAsync(ct).ConfigureAwait(false);

                    if (!ExternalServiceResource.UrlIsValidForExternalService(url, out uri, out var message))
                    {
                        e.Logger.LogError("The value for URL parameter '{ParameterName}' is invalid: {Error}", resource.UrlParameter?.Name, message);

                        await e.Notifications.PublishUpdateAsync(resource, snapshot => snapshot with
                        {
                            State = KnownResourceStates.FailedToStart
                        }).ConfigureAwait(false);

                        return;
                    }
                }

                Debug.Assert(uri is not null, "URI must be set at this point.");

                await e.Eventing.PublishAsync(new BeforeResourceStartedEvent(e.Resource, e.Services), ct).ConfigureAwait(false);

                await e.Notifications.PublishUpdateAsync(resource, snapshot => snapshot with
                {
                    Properties = [
                        new(CustomResourceKnownProperties.Source, uri.Host)
                    ],
                    // Add the URL if it came from a parameter as non-static URLs must be published by the owning custom resource
                    Urls = AddUrlIfNotPresent(snapshot.Urls, uri),
                    // Required in order for health checks to work
                    State = KnownResourceStates.Running
                }).ConfigureAwait(false);

                static ImmutableArray<UrlSnapshot> AddUrlIfNotPresent(ImmutableArray<UrlSnapshot> urlSnapshots, Uri uri)
                {
                    if (urlSnapshots.Any(u => string.Equals(u.Url, uri.ToString(), StringComparisons.Url)))
                    {
                        return urlSnapshots; // URL already exists, no need to add it again
                    }

                    return urlSnapshots.Add(new(Name: null, uri.ToString(), IsInternal: false));
                }
            }
        });

        return resourceBuilder;
    }

    /// <summary>
    /// Adds a health check to the external service resource.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="path">The relative path to use for the HTTP health check.</param>
    /// <param name="statusCode"></param>
    /// <returns></returns>
    /// <remarks>
    /// <para>
    /// This method adds a health check to the health check service which polls the specified external service
    /// on a periodic basis. The address is based on the URL of the external service.
    /// A path for the health check request can be specified. The expected status code is set to <c>200</c> by default but a
    /// different one can be specified.
    /// </para>
    /// </remarks>
    public static IResourceBuilder<ExternalServiceResource> WithHttpHealthCheck(this IResourceBuilder<ExternalServiceResource> builder, string? path = null, int? statusCode = null)
    {
        if (path is not null && !Uri.IsWellFormedUriString(path, UriKind.Relative))
        {
            throw new ArgumentException($"The path '{path}' is not a valid relative URL.", nameof(path));
        }

        statusCode ??= 200;

        var pathKey = path is not null ? $"_{path}" : string.Empty;
        var healthCheckKey = $"{builder.Resource.Name}_external{pathKey}_{statusCode}_check";

        builder.ApplicationBuilder.Services.SuppressHealthCheckHttpClientLogging(healthCheckKey);

        builder.ApplicationBuilder.Services.AddHealthChecks().AddUrlGroup(options =>
        {
            var uri = builder.Resource.Uri;

            // OK accessing the parameter here synchronously as this should only activate once the resource is running

            if (uri is null && !Uri.TryCreate(builder.Resource.UrlParameter?.Value, UriKind.Absolute, out uri)
                || (uri?.Scheme != "http" && uri?.Scheme != "https"))
            {
                return; // Skip health check if the URI is not set or not HTTP/HTTPS
            }

            if (path is not null)
            {
                uri = new Uri(uri, path);
            }

            options.AddUri(uri, setup => setup.ExpectHttpCode(statusCode.Value));
        }, healthCheckKey);

        builder.WithHealthCheck(healthCheckKey);

        return builder;
    }
}
