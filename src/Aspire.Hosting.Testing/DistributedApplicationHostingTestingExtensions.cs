// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing.Properties;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting.Testing;

/// <summary>
/// Extensions for working with <see cref="DistributedApplication"/> in test code.
/// </summary>
public static class DistributedApplicationHostingTestingExtensions
{
    /// <summary>
    /// Creates an <see cref="HttpClient"/> configured to communicate with the specified resource.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <param name="resourceName">The resourceName of the resource.</param>
    /// <param name="endpointName">The resourceName of the endpoint on the resource to communicate with.</param>
    /// <returns>The <see cref="HttpClient"/>.</returns>
    public static HttpClient CreateHttpClient(this DistributedApplication app, string resourceName, string? endpointName = default)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentException.ThrowIfNullOrEmpty(resourceName);

        var baseUri = GetEndpointUriStringCore(app, resourceName, endpointName);
        var clientFactory = app.Services.GetRequiredService<IHttpClientFactory>();
        var client = clientFactory.CreateClient();
        client.BaseAddress = new(baseUri);

        return client;
    }

    /// <summary>
    /// Gets the connection string for the specified resource.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <param name="resourceName">The resource name.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>The connection string for the specified resource.</returns>
    /// <exception cref="ArgumentException">The resource was not found or does not expose a connection string.</exception>
    public static ValueTask<string?> GetConnectionStringAsync(this DistributedApplication app, string resourceName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentException.ThrowIfNullOrEmpty(resourceName);

        var resource = GetResource(app, resourceName);
        if (resource is not IResourceWithConnectionString resourceWithConnectionString)
        {
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.ResourceDoesNotExposeConnectionStringExceptionMessage, resourceName), nameof(resourceName));
        }

        return resourceWithConnectionString.GetConnectionStringAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the endpoint for the specified resource.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <param name="resourceName">The resource name.</param>
    /// <param name="endpointName">The optional endpoint name. If none are specified, the single defined endpoint is returned.</param>
    /// <returns>A URI representation of the endpoint.</returns>
    /// <exception cref="ArgumentException">The resource was not found, no matching endpoint was found, or multiple endpoints were found.</exception>
    /// <exception cref="InvalidOperationException">The resource has no endpoints.</exception>
    public static Uri GetEndpoint(this DistributedApplication app, string resourceName, string? endpointName = default)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentException.ThrowIfNullOrEmpty(resourceName);

        return new(GetEndpointUriStringCore(app, resourceName, endpointName));
    }

    static IResource GetResource(DistributedApplication app, string resourceName)
    {
        ThrowIfNotStarted(app);
        var applicationModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resources = applicationModel.Resources;
        var resource = resources.SingleOrDefault(r => string.Equals(r.Name, resourceName, StringComparison.OrdinalIgnoreCase));

        if (resource is null)
        {
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.ResourceNotFoundExceptionMessage, resourceName), nameof(resourceName));
        }

        return resource;
    }

    static string GetEndpointUriStringCore(DistributedApplication app, string resourceName, string? endpointName = default)
    {
        var resource = GetResource(app, resourceName);
        if (resource is not IResourceWithEndpoints resourceWithEndpoints)
        {
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.ResourceHasNoAllocatedEndpointsExceptionMessage, resourceName), nameof(resourceName));
        }

        EndpointReference? endpoint;
        if (!string.IsNullOrEmpty(endpointName))
        {
            endpoint = GetEndpointOrDefault(resourceWithEndpoints, endpointName);
        }
        else
        {
            endpoint = GetEndpointOrDefault(resourceWithEndpoints, "http") ?? GetEndpointOrDefault(resourceWithEndpoints, "https");
        }

        if (endpoint is null)
        {
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.EndpointForResourceNotFoundExceptionMessage, endpointName, resourceName), nameof(endpointName));
        }

        return endpoint.Url;
    }

    static void ThrowIfNotStarted(DistributedApplication app)
    {
        var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
        if (!lifetime.ApplicationStarted.IsCancellationRequested)
        {
            throw new InvalidOperationException(Resources.ApplicationNotStartedExceptionMessage);
        }
    }

    static EndpointReference? GetEndpointOrDefault(IResourceWithEndpoints resourceWithEndpoints, string endpointName)
    {
        var reference = resourceWithEndpoints.GetEndpoint(endpointName);

        return reference.IsAllocated ? reference : null;
    }
}
