// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Tests.Helpers;

public static class AllocatedEndpointAnnotationTestExtensions
{
    /// <summary>
    /// Sends a GET request to the specified resource and returns the response body as a string.
    /// </summary>
    /// <typeparam name="T">The type of the resource.</typeparam>
    /// <param name="builder">The resource.</param>
    /// <param name="client">The <see cref="HttpClient"/> instance to use.</param>
    /// <param name="bindingName">The name of the binding.</param>
    /// <param name="path">The path the request is sent to.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The string representing the response body.</returns>
    public static async Task<string> HttpGetStringAsync<T>(this IResourceBuilder<T> builder, HttpClient client, string bindingName, string path, CancellationToken cancellationToken)
        where T : IResourceWithBindings
    {
        var allocatedEndpoint = builder.Resource.Annotations.OfType<AllocatedEndpointAnnotation>().Single(a => a.Name == bindingName);
        var url = $"{allocatedEndpoint.UriString}{path}";

        var response = await client.GetStringAsync(url, cancellationToken);
        return response;
    }

    /// <summary>
    /// Sends a GET request to the specified resource and returns the response message.
    /// </summary>
    /// <typeparam name="T">The type of the resource.</typeparam>
    /// <param name="builder">The resource.</param>
    /// <param name="client">The <see cref="HttpClient"/> instance to use.</param>
    /// <param name="bindingName">The name of the binding.</param>
    /// <param name="path">The path the request is sent to.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The response message.</returns>
    public static async Task<HttpResponseMessage> HttpGetAsync<T>(this IResourceBuilder<T> builder, HttpClient client, string bindingName, string path, CancellationToken cancellationToken)
        where T : IResourceWithBindings
    {
        var allocatedEndpoint = builder.Resource.Annotations.OfType<AllocatedEndpointAnnotation>().Single(a => a.Name == bindingName);
        var url = $"{allocatedEndpoint.UriString}{path}";

        var response = await client.GetAsync(url, cancellationToken);
        return response;
    }

    /// <summary>
    /// Sends a POST request to the specified resource and returns the response message.
    /// </summary>
    /// <typeparam name="T">The type of the resource.</typeparam>
    /// <param name="builder">The resource.</param>
    /// <param name="client">The <see cref="HttpClient"/> instance to use.</param>
    /// <param name="bindingName">The name of the binding.</param>
    /// <param name="path">The path the request is sent to.</param>
    /// <param name="content">The HTTP request content sent to the server.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The response message.</returns>
    public static async Task<HttpResponseMessage> HttpPostAsync<T>(this IResourceBuilder<T> builder, HttpClient client, string bindingName, string path, HttpContent? content, CancellationToken cancellationToken)
        where T : IResourceWithBindings
    {
        var allocatedEndpoint = builder.Resource.Annotations.OfType<AllocatedEndpointAnnotation>().Single(a => a.Name == bindingName);
        var url = $"{allocatedEndpoint.UriString}{path}";

        var response = await client.PostAsync(url, content, cancellationToken);
        return response;
    }

    public static Task<string> WaitForHealthyStatusAsync(this IResourceBuilder<ProjectResource> builder, HttpClient client, string bindingName, CancellationToken cancellationToken)
    {
        return HttpGetStringWithRetryAsync(builder, client, bindingName, "/health", cancellationToken);
    }

    public static Task<string> HttpGetPidAsync<T>(this IResourceBuilder<T> builder, HttpClient client, string bindingName, CancellationToken cancellationToken)
        where T : IResourceWithBindings
    {
        return HttpGetStringWithRetryAsync(builder, client, bindingName, "/pid", cancellationToken);
    }

    public static async Task<string> HttpGetStringWithRetryAsync<T>(this IResourceBuilder<T> builder, HttpClient client, string bindingName, string request, CancellationToken cancellationToken)
        where T : IResourceWithBindings
    {
        while (true)
        {
            try
            {
                return await builder.HttpGetStringAsync(client, bindingName, request, cancellationToken);
            }
            catch
            {
                await Task.Delay(100, cancellationToken);
            }
        }
    }
}
