// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace Aspire.Hosting.Testing.Tests;

internal static class DistributedApplicationHttpClientExtensionsForTests
{
    private static readonly Lazy<IHttpClientFactory> s_httpClientFactory = new(CreateHttpClientFactoryWithResilience);
    public static HttpClient CreateHttpClientWithResilience(this DistributedApplication app, string resourceName, string? endpointName = default, Action<HttpStandardResilienceOptions>? configure = default)
    {
        var baseUri = app.GetEndpoint(resourceName, endpointName);
        HttpClient client;
        if (configure is not null)
        {
            var factory = CreateHttpClientFactoryWithResilience(configure);
            client = factory.CreateClient();
        }
        else
        {
            client = s_httpClientFactory.Value.CreateClient();
        }
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

    private static IHttpClientFactory CreateHttpClientFactoryWithResilience(Action<HttpStandardResilienceOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddHttpClient()
            .ConfigureHttpClientDefaults(b =>
            {
                b.AddStandardResilienceHandler(configure);
            });

        return services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
    }
}
