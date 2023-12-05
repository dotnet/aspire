// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Tests.Helpers;
public static class AllocatedEndpointAnnotationTestExtensions
{
    public static async Task<string> HttpGetAsync<T>(this IResourceBuilder<T> builder, HttpClient client, string bindingName, string path, CancellationToken cancellationToken)
        where T : IResourceWithBindings
    {
        // We have to get the allocated endpoint each time through the loop
        // because it may not be populated yet by the time we get here.
        var allocatedEndpoint = builder.Resource.Annotations.OfType<AllocatedEndpointAnnotation>().Single(a => a.Name == bindingName);
        var url = $"{allocatedEndpoint.UriString}{path}";

        var response = await client.GetStringAsync(url, cancellationToken);
        return response;
    }

    public static Task<string> WaitForHealthyStatus(this IResourceBuilder<ProjectResource> builder, HttpClient client, string bindingName, CancellationToken cancellationToken)
    {
        return HttpGetWithRetryAsync(builder, client, bindingName, "/health", cancellationToken);
    }

    public static Task<string> HttpGetPidAsync<T>(this IResourceBuilder<T> builder, HttpClient client, string bindingName, CancellationToken cancellationToken)
        where T : IResourceWithBindings
    {
        return HttpGetWithRetryAsync(builder, client, bindingName, "/pid", cancellationToken);
    }

    public static async Task<string> HttpGetWithRetryAsync<T>(this IResourceBuilder<T> builder, HttpClient client, string bindingName, string request, CancellationToken cancellationToken)
        where T : IResourceWithBindings
    {
        while (true)
        {
            try
            {
                return await builder.HttpGetAsync(client, bindingName, request, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(ex);
            }
            catch
            {
                await Task.Delay(100, cancellationToken);
            }
        }
    }
}
