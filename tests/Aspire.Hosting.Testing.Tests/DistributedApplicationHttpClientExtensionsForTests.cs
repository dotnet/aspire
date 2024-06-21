// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Testing.Tests;

internal static class DistributedApplicationHttpClientExtensionsForTests
{
    private static readonly Lazy<IHttpClientFactory> s_httpClientFactory = new(CreateHttpClientFactoryWithResilience);
    public static HttpClient CreateHttpClientWithResilience(this DistributedApplication app, string resourceName, string? endpointName = default)
    {
        var baseUri = app.GetEndpoint(resourceName, endpointName);
        var client = s_httpClientFactory.Value.CreateClient();
        client.BaseAddress = baseUri;
        return client;
    }

    private static IHttpClientFactory CreateHttpClientFactoryWithResilience()
    {
        var services = new ServiceCollection();
        services.AddHttpClient()
            .ConfigureHttpClientDefaults(b =>
            {
                b.AddStandardResilienceHandler();
            });

        return services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
    }
}
