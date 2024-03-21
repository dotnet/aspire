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
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>The string representing the response body.</returns>
    public static async Task<string> HttpGetStringAsync<T>(this IResourceBuilder<T> builder, HttpClient client, string bindingName, string path, CancellationToken cancellationToken)
        where T : IResourceWithEndpoints
    {
        var endpoint = builder.Resource.GetEndpoint(bindingName);
        var url = $"{endpoint.Url}{path}";

        var response = await client.GetStringAsync(url, cancellationToken);
        return response;
    }

    public static Task<string> HttpGetPidAsync<T>(this IResourceBuilder<T> builder, HttpClient client, string bindingName, CancellationToken cancellationToken)
        where T : IResourceWithEndpoints
    {
        return HttpGetStringWithRetryAsync(builder, client, bindingName, "/pid", cancellationToken);
    }

    public static async Task<string> HttpGetStringWithRetryAsync<T>(this IResourceBuilder<T> builder, HttpClient client, string bindingName, string request, CancellationToken cancellationToken)
        where T : IResourceWithEndpoints
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
