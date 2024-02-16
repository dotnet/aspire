// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Testing;

/// <summary>
/// Extensions for working with <see cref="DistributedApplication"/> in test code.
/// </summary>
public static class DistributedApplicationExtensions
{
    /// <summary>
    /// Creates an <see cref="HttpClient"/> configured to communicate with the specified resource.
    /// </summary>
    /// <param resourceName="app">The application.</param>
    /// <param resourceName="resourceName">The resourceName of the resource.</param>
    /// <param resourceName="endpointName">The resourceName of the endpoint on the resource to communicate with.</param>
    /// <returns>The <see cref="HttpClient"/>.</returns>
    public static HttpClient CreateHttpClient(this DistributedApplication app, string resourceName, string? endpointName = default)
    {
        var applicationModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resources = applicationModel.Resources;
        var resource = resources.FirstOrDefault(r => string.Equals(r.Name, resourceName, StringComparison.OrdinalIgnoreCase));

        if (resource is null)
        {
            throw new ArgumentException($"Resource '{resourceName}' not found", nameof(resourceName));
        }

        if (!resource.TryGetAllocatedEndPoints(out var endpoints))
        {
            throw new InvalidOperationException($"Cannot create a client for resource '{resourceName}' because it has no allocated endpoints.");
        }

        AllocatedEndpointAnnotation? endpoint = null;

        if (!string.IsNullOrEmpty(endpointName))
        {
            endpoint = endpoints.FirstOrDefault(e => string.Equals(e.Name, endpointName, StringComparison.OrdinalIgnoreCase));

            if (endpoint is null)
            {
                throw new ArgumentException($"Endpoint '{endpointName}' for resource '{resourceName}' not found", nameof(endpointName));
            }
        }
        else
        {
            endpoint = endpoints.FirstOrDefault(e =>
                string.Equals(e.UriScheme, "http", StringComparison.OrdinalIgnoreCase) || string.Equals(e.UriScheme, "https", StringComparison.OrdinalIgnoreCase));

            if (endpoint is null)
            {
                throw new InvalidOperationException($"Cannot create a client for resource '{resourceName}' because it has no allocated HTTP endpoints.");
            }
        }

        var clientFactory = app.Services.GetRequiredService<IHttpClientFactory>();

        var client = clientFactory.CreateClient();
        client.BaseAddress = new(endpoint.UriString);

        return client;
    }

    /// <summary>
    /// Gets the connection string for the specified resource.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <param name="resourceName">The resource name.</param>
    /// <returns>The connection string for the specified resource.</returns>
    /// <exception cref="ArgumentException">The resource was not found or does not expose a connection string.</exception>
    public static string? GetConnectionString(this DistributedApplication app, string resourceName)
    {
        var applicationModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var resource = applicationModel.Resources.FirstOrDefault(r => string.Equals(r.Name, resourceName, StringComparison.OrdinalIgnoreCase));

        if (resource is null)
        {
            throw new ArgumentException($"Resource '{resourceName}' not found.", nameof(resourceName));
        }

        if (resource is not IResourceWithConnectionString resourceWithConnectionString)
        {
            throw new ArgumentException($"Resource '{resourceName}' does not expose a connection string.", nameof(resourceName));
        }

        return resourceWithConnectionString.GetConnectionString();
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
        var applicationModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resources = applicationModel.Resources;
        var resource = resources.FirstOrDefault(r => string.Equals(r.Name, resourceName, StringComparison.OrdinalIgnoreCase));

        if (resource is null)
        {
            throw new ArgumentException($"Resource '{resourceName}' not found", nameof(resourceName));
        }

        if (!resource.TryGetAllocatedEndPoints(out var endpoints))
        {
            throw new InvalidOperationException($"Resource '{resourceName}' has no allocated endpoints.");
        }

        AllocatedEndpointAnnotation? endpoint = null;
        var endpointList = endpoints.ToList();
        if (!string.IsNullOrEmpty(endpointName))
        {
            endpoint = endpointList.FirstOrDefault(e => string.Equals(e.Name, endpointName, StringComparison.OrdinalIgnoreCase));

            if (endpoint is null)
            {
                throw new ArgumentException($"Resource '{resourceName}' does not have an endpoint named '{endpointName}'.", nameof(endpointName));
            }
        }
        else
        {
            if (endpointList.Count > 1)
            {
                throw new ArgumentException($"Resource '{resourceName}' has multiple endpoints but no endpoint name was specified.", nameof(endpointName));
            }

            endpoint = endpointList[0];
        }

        return new(endpoint.UriString);
    }
}
