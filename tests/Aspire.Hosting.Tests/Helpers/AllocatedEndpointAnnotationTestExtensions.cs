// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Tests.Helpers;
public static class AllocatedEndpointAnnotationTestExtensions
{
    public static async Task<string> HttpGetPidAsync(this IResourceBuilder<ProjectResource> builder, HttpClient client, string bindingName, CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                // We have to get the allocated endpoint each time through the loop
                // because it may not be populated yet by the time we get here.
                var allocatedEndpoint = builder.Resource.Annotations.OfType<AllocatedEndpointAnnotation>().Single(a => a.Name == bindingName);
                var url = $"{allocatedEndpoint.UriString}/pid";

                var response = await client.GetStringAsync(url, cancellationToken);
                return response;
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
