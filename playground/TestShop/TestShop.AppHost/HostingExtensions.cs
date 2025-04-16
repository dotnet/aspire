// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace TestShop.AppHost;

internal static class HostingExtensions
{
    /// <summary>
    /// Adds a rference to the dashboard resource so that the required environment variables to allow service discovery to the Aspire dashboard
    /// are added to this resource. Also configures a reference relationship to the dashboard resource.
    /// </summary>
    public static IResourceBuilder<T> WithDashboardReference<T>(this IResourceBuilder<T> builder, IResourceBuilder<ProjectResource> dashboard, string? proxyPath = null)
        where T : IResourceWithEnvironment
    {
        if (dashboard.Resource.Name != KnownResourceNames.AspireDashboard)
        {
            throw new ArgumentException($"The dashboard resource must be named '{KnownResourceNames.AspireDashboard}'.", nameof(dashboard));
        }

        var dashboardUris = builder.ApplicationBuilder.Configuration["ASPNETCORE_URLS"]?.Split(';')
            ?.Select(u => Uri.TryCreate(u, UriKind.Absolute, out var uri) is { } ? uri : null)
            ?.Where(u => u is not null)
            ?.Cast<Uri>()
            ?.GroupBy(u => u.Scheme)
            ?? [];

        foreach (var scheme in dashboardUris)
        {
            var urisForScheme = scheme.ToArray();
            for (var i = 0; i < urisForScheme.Length; i++)
            {
                builder.WithEnvironment($"services__aspire-dashboard__{scheme.Key}__{i}", urisForScheme[i].ToString());
            }
        }

        if (proxyPath is not null)
        {
            builder
                .WithEnvironment("DASHBOARD_PATH", proxyPath)
                .WithUrls(context =>
                {
                    var https = context.Urls.FirstOrDefault(u => u.Endpoint?.EndpointName == "https");
                    var http = context.Urls.FirstOrDefault(u => u.Endpoint?.EndpointName == "http");
                    if (https is not null || http is not null)
                    {
                        var endpoint = https ?? http!;
                        context.Urls.Add(new() { Url = $"{endpoint.Url}{proxyPath}", DisplayText = "Proxied Dashboard" });
                    }
                });
        }

        builder.WithReferenceRelationship(dashboard);

        return builder;
    }
}
