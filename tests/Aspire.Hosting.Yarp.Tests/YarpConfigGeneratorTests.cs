// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Authentication;
using System.Text.Json.Nodes;
using Xunit;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.LoadBalancing;

namespace Aspire.Hosting.Yarp.Tests;

public class YarpConfigGeneratorTests()
{
    #region JSON test configuration

    private const string ValidJsonConfig =
        """
        {
            "ReverseProxy":
            {
                "Clusters": {
                    "cluster1": {
                        "LoadBalancingPolicy": "Random",
                        "SessionAffinity": {
                            "Enabled": true,
                            "Policy": "Cookie",
                            "FailurePolicy": "Return503Error",
                            "AffinityKeyName": "Key1",
                            "Cookie": {
                                "Domain": "localhost",
                                "Expiration": "03:00:00",
                                "HttpOnly": true,
                                "IsEssential": true,
                                "MaxAge": "1.00:00:00",
                                "Path": "mypath",
                                "SameSite": "Strict",
                                "SecurePolicy": "None"
                            }
                        },
                        "HealthCheck": {
                            "Passive": {
                                "Enabled": true,
                                "Policy": "FailureRate",
                                "ReactivationPeriod": "00:05:00"
                            },
                            "Active": {
                                "Enabled": true,
                                "Interval": "00:00:04",
                                "Timeout": "00:00:06",
                                "Policy": "Any5xxResponse",
                                "Path": "healthCheckPath",
                                "Query": "?key=value"
                            },
                            "AvailableDestinationsPolicy": "HealthyOrPanic"
                        },
                        "HttpClient": {
                            "SslProtocols": [
                                "Tls12",
                                "Tls13"
                            ],
                            "DangerousAcceptAnyServerCertificate": true,
                            "MaxConnectionsPerServer": 10,
                            "EnableMultipleHttp2Connections": true,
                            "RequestHeaderEncoding": "utf-8",
                            "ResponseHeaderEncoding": "utf-8",
                            "WebProxy": {
                                "Address": "http://localhost:8080",
                                "BypassOnLocal": true,
                                "UseDefaultCredentials": true
                            }
                        },
                        "HttpRequest": {
                            "ActivityTimeout": "00:01:00",
                            "Version": "1.0",
                            "VersionPolicy": "RequestVersionExact",
                            "AllowResponseBuffering": true
                        },
                        "Destinations": {
                            "destinationA": {
                                "Address": "https://localhost:10000/destA",
                                "Health": "https://localhost:20000/destA",
                                "Host": "localhost",
                                "Metadata": {
                                    "destA-K1": "destA-V1",
                                    "destA-K2": "destA-V2"
                                }
                            },
                            "destinationB": {
                                "Address": "https://localhost:10000/destB",
                                "Health": "https://localhost:20000/destB",
                                "Host": "localhost",
                                "Metadata": {
                                    "destB-K1": "destB-V1",
                                    "destB-K2": "destB-V2"
                                }
                            }
                        },
                        "Metadata": {
                            "cluster1-K1": "cluster1-V1",
                            "cluster1-K2": "cluster1-V2"
                        }
                    },
                    "cluster2": {
                        "LoadBalancingPolicy": "RoundRobin",
                        "Destinations": {
                            "destinationC": {
                                "Address": "https://localhost:10001/destC",
                                "Host": "localhost"
                            },
                            "destinationD": {
                                "Address": "https://localhost:10000/destB",
                                "Host": "remotehost"
                            }
                        }
                    }
                },
                "Routes": {
                    "routeA" : {
                        "Match": {
                            "Methods": [
                                "GET",
                                "POST",
                                "DELETE"
                            ],
                            "Hosts": [
                                "host-A"
                            ],
                            "Path": "/apis/entities",
                            "Headers": [
                              {
                                "Name": "header1",
                                "Values": [ "value1" ],
                                "IsCaseSensitive": true,
                                "Mode": "HeaderPrefix"
                              }
                            ],
                            "QueryParameters": [
                              {
                                "Name": "queryparam1",
                                "Values": [ "value1" ],
                                "IsCaseSensitive": true,
                                "Mode": "Contains"
                              }
                            ]
                        },
                        "Order": -1,
                        "MaxRequestBodySize": -1,
                        "ClusterId": "cluster1",
                        "AuthorizationPolicy": "Default",
                        "RateLimiterPolicy": "Default",
                        "OutputCachePolicy": "Default",
                        "CorsPolicy": "Default",
                        "TimeoutPolicy": "Default",
                        "Timeout": "00:00:01",
                        "Metadata": {
                            "routeA-K1": "routeA-V1",
                            "routeA-K2": "routeA-V2"
                        },
                        "Transforms": [
                            {
                                "RequestHeadersCopy": "true"
                            },
                            {
                                "PathRemovePrefix": "/apis"
                            },
                            {
                                "PathPrefix": "/apis"
                            },
                            {
                                "RequestHeader": "header1",
                                "Append": "foo"
                            }
                        ]
                    },
                    "routeB" : {
                        "Match": {
                            "Methods": [
                                "GET"
                            ],
                            "Hosts": [
                                "host-B"
                            ],
                            "Path": "/apis/users",
                            "Headers": [
                              {
                                "IsCaseSensitive": false,
                                "Mode": "ExactHeader",
                                "Name": "header2",
                                "Values": [ "value2" ]
                              }
                            ],
                            "QueryParameters": [
                              {
                                "Name": "queryparam2",
                                "Values": [ "value2" ],
                                "IsCaseSensitive": true,
                                "Mode": "Contains"
                              }
                            ]
                        },
                        "Order": 2,
                        "MaxRequestBodySize": 1,
                        "ClusterId": "cluster2"
                    }
                }
            }
        }
        """;

    private readonly List<RouteConfig> _validRoutes =
    [
        new RouteConfig
        {
            RouteId = "routeA",
            ClusterId = "cluster1",
            AuthorizationPolicy = "Default",
            RateLimiterPolicy = "Default",
            TimeoutPolicy = "Default",
            Timeout = TimeSpan.FromSeconds(1),
            CorsPolicy = "Default",
            Order = -1,
            MaxRequestBodySize = -1,
            OutputCachePolicy = "Default",
            Match = new RouteMatch
            {
                Hosts = new List<string> { "host-A" },
                Methods = new List<string> { "GET", "POST", "DELETE" },
                Path = "/apis/entities",
                Headers = new[]
                {
                    new RouteHeader
                    {
                        Name = "header1",
                        Values = new[] { "value1" },
                        IsCaseSensitive = true,
                        Mode = HeaderMatchMode.HeaderPrefix
                    }
                },
                QueryParameters = new[]
                {
                    new RouteQueryParameter
                    {
                        Name = "queryparam1",
                        Values = new[] { "value1" },
                        IsCaseSensitive = true,
                        Mode = QueryParameterMatchMode.Contains
                    }
                }
            },
            Transforms = new[]
            {
                new Dictionary<string, string>
                {
                    { "RequestHeadersCopy", "true" }
                },
                new Dictionary<string, string>
                {
                    { "PathRemovePrefix", "/apis" },
                },
                new Dictionary<string, string>
                {
                    { "PathPrefix", "/apis" }
                },
                new Dictionary<string, string>
                {
                    { "RequestHeader", "header1" },
                    { "Append", "foo" }
                }
            },
            Metadata = new Dictionary<string, string> { { "routeA-K1", "routeA-V1" }, { "routeA-K2", "routeA-V2" } }
        },
        new RouteConfig
        {
            RouteId = "routeB",
            ClusterId = "cluster2",
            Order = 2,
            MaxRequestBodySize = 1,
            Match = new RouteMatch
            {
                Hosts = new List<string> { "host-B" },
                Methods = new List<string> { "GET" },
                Path = "/apis/users",
                Headers = new[]
                {
                    new RouteHeader
                    {
                        Name = "header2",
                        Values = new[] { "value2" },
                        IsCaseSensitive = false,
                        Mode = HeaderMatchMode.ExactHeader
                    }
                },
                QueryParameters = new[]
                {
                    new RouteQueryParameter
                    {
                        Name = "queryparam2",
                        Values = new[] { "value2" },
                        IsCaseSensitive = true,
                        Mode = QueryParameterMatchMode.Contains
                    }
                }
            }
        }
    ];

    private readonly List<ClusterConfig> _validClusters =
    [
        new ClusterConfig
        {
            ClusterId = "cluster1",
            Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
                    {
                        {
                            "destinationA",
                            new DestinationConfig
                            {
                                Address = "https://localhost:10000/destA",
                                Health = "https://localhost:20000/destA",
                                Metadata = new Dictionary<string, string> { { "destA-K1", "destA-V1" }, { "destA-K2", "destA-V2" } },
                                Host = "localhost"
                            }
                        },
                        {
                            "destinationB",
                            new DestinationConfig
                            {
                                Address = "https://localhost:10000/destB",
                                Health = "https://localhost:20000/destB",
                                Metadata = new Dictionary<string, string> { { "destB-K1", "destB-V1" }, { "destB-K2", "destB-V2" } },
                                Host = "localhost"
                            }
                        }
                    },
            HealthCheck = new HealthCheckConfig
            {
                Passive = new PassiveHealthCheckConfig
                {
                    Enabled = true,
                    Policy = "FailureRate",
                    ReactivationPeriod = TimeSpan.FromMinutes(5)
                },
                Active = new ActiveHealthCheckConfig
                {
                    Enabled = true,
                    Interval = TimeSpan.FromSeconds(4),
                    Timeout = TimeSpan.FromSeconds(6),
                    Policy = "Any5xxResponse",
                    Path = "healthCheckPath",
                    Query = "?key=value"
                },
                AvailableDestinationsPolicy = "HealthyOrPanic"
            },
            LoadBalancingPolicy = LoadBalancingPolicies.Random,
            SessionAffinity = new SessionAffinityConfig
            {
                Enabled = true,
                FailurePolicy = "Return503Error",
                Policy = "Cookie",
                AffinityKeyName = "Key1",
                Cookie = new SessionAffinityCookieConfig
                {
                    Domain = "localhost",
                    Expiration = TimeSpan.FromHours(3),
                    HttpOnly = true,
                    IsEssential = true,
                    MaxAge = TimeSpan.FromDays(1),
                    Path = "mypath",
                    SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                    SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.None
                }
            },
            HttpClient = new HttpClientConfig
            {
                SslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12,
                MaxConnectionsPerServer = 10,
                DangerousAcceptAnyServerCertificate = true,
                EnableMultipleHttp2Connections = true,
                RequestHeaderEncoding = "utf-8",
                ResponseHeaderEncoding = "utf-8",
                WebProxy = new WebProxyConfig()
                {
                    Address = new Uri("http://localhost:8080"),
                    BypassOnLocal = true,
                    UseDefaultCredentials = true,
                }
            },
            HttpRequest = new ForwarderRequestConfig()
            {
                ActivityTimeout = TimeSpan.FromSeconds(60),
                Version = Version.Parse("1.0"),
                VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                AllowResponseBuffering = true
            },
            Metadata = new Dictionary<string, string> { { "cluster1-K1", "cluster1-V1" }, { "cluster1-K2", "cluster1-V2" } }
        },
        new ClusterConfig
        {
            ClusterId = "cluster2",
            Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
            {
                { "destinationC", new DestinationConfig { Address = "https://localhost:10001/destC", Host = "localhost" } },
                { "destinationD", new DestinationConfig { Address = "https://localhost:10000/destB", Host = "remotehost" } }
            },
            LoadBalancingPolicy = LoadBalancingPolicies.RoundRobin
        }
    ];

    #endregion

    [Fact]
    public async Task GenerateConfiguration()
    {
        var config = new YarpConfigurationBuilder();
        foreach (var cluster in _validClusters)
        {
            config.AddCluster(cluster);
        }
        foreach (var routes in _validRoutes)
        {
            config.AddRoute(routes);
        }

        var content = await config.Build(CancellationToken.None);
        Assert.NotEmpty(content);

        var actual = JsonNode.Parse(content);
        var expected = JsonNode.Parse(ValidJsonConfig);

        Assert.True(JsonNode.DeepEquals(expected, actual));
    }
}
