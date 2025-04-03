// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateSlimBuilder(args);

builder.AddServiceDefaults();

builder.WebHost.UseKestrelHttpsConfiguration();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddConfigFilter<SetDestinationPathConfigFilter>()
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

app.UseRouting();

app.MapGet("/", (IConfiguration configuration) => $"Dashboard is hosted at '{configuration["DASHBOARD_PATH"]}'");
app.MapReverseProxy();

app.Run();

internal sealed class SetDestinationPathConfigFilter(IConfiguration configuration) : IProxyConfigFilter
{
    private readonly string _dashboardPath = (configuration["DASHBOARD_PATH"] ?? "/dashboard").TrimEnd('/');

    public ValueTask<RouteConfig> ConfigureRouteAsync(RouteConfig route, ClusterConfig? cluster, CancellationToken cancel)
    {
        var newRoute = route;
        if (route.RouteId == "dashboard")
        {
            // "Match": {
            //      "Path": "/dashboard/{**catch-all}"
            //    },
            //    "Transforms": [
            //      { "PathRemovePrefix": "/dashboard" },
            //      {
            //        "X-Forwarded": "Set",
            //        "Prefix": "Off"
            //      },
            //      {
            //        "RequestHeader": "X-Forwarded-Prefix",
            //        "Set": "/dashboard"
            //      }
            //    ]
            var newTransforms = new List<IReadOnlyDictionary<string, string>>();
            if (route.Transforms is { } transforms)
            {
                foreach (var transform in transforms)
                {
                    var newTransform = new Dictionary<string, string>(transform);
                    if (newTransform.ContainsKey("PathRemovePrefix"))
                    {
                        newTransform["PathRemovePrefix"] = _dashboardPath;
                    }
                    else if (newTransform.TryGetValue("RequestHeader", out var requestHeader)
                             && requestHeader == "X-Forwarded-Prefix")
                    {
                        newTransform["Set"] = _dashboardPath;
                    }
                    newTransforms.Add(newTransform);
                }
            }
            newRoute = route with
            {
                Match = route.Match with
                {
                    Path = $"{_dashboardPath}/{{**catch-all}}",
                },
                Transforms = newTransforms
            };
        }
        return ValueTask.FromResult(newRoute);
    }

    public ValueTask<ClusterConfig> ConfigureClusterAsync(ClusterConfig cluster, CancellationToken cancel)
    {
        return ValueTask.FromResult(cluster);
    }
}
