// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Testing.Tests;

internal static class Extensions
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
                b.ConfigureHttpClient(client =>
                {
                    // Disable the HttpClient timeout to allow the timeout strategies to control the timeout.
                    client.Timeout = Timeout.InfiniteTimeSpan;
                });

                b.UseSocketsHttpHandler((handler, sp) =>
                {
                    handler.PooledConnectionLifetime = TimeSpan.FromSeconds(5);
                    handler.ConnectTimeout = TimeSpan.FromSeconds(5);
                });

                // Ensure transient errors are retried for up to 5 minutes
                b.AddStandardResilienceHandler(options =>
                {
                    options.AttemptTimeout.Timeout = TimeSpan.FromMinutes(2);
                    options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(5); // needs to be at least double the AttemptTimeout to pass options validation
                    options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(10);
                    options.Retry.OnRetry = async (args) =>
                    {
                        var msg = $"Retry #{args.AttemptNumber+1} for '{args.Outcome.Result?.RequestMessage?.RequestUri}'" +
                                        $" due to StatusCode: {(int?)args.Outcome.Result?.StatusCode} ReasonPhrase: '{args.Outcome.Result?.ReasonPhrase}'";

                        msg += (args.Outcome.Exception is not null) ? $" Exception: {args.Outcome.Exception} " : "";
                        if (args.Outcome.Result?.Content is HttpContent content && (await content.ReadAsStringAsync().ConfigureAwait(false)) is string contentStr)
                        {
                            msg += $" Content:{Environment.NewLine}{contentStr}";
                        }
                        Console.WriteLine(msg);
                    };
                    options.Retry.MaxRetryAttempts = 20;
                });
            });

        return services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
    }
}
