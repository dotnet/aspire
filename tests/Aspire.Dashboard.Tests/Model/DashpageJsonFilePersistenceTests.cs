// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Xunit;
using static Aspire.Dashboard.Components.Pages.Metrics;

namespace Aspire.Dashboard.Tests.Model;

public sealed class DashpageJsonFilePersistenceTests
{
    [Fact]
    public void Deserialize()
    {
        var json = """
            [
                /* Comments are ignored */
                {
                    "name": ".NET",
                    "priority": 99,
                    "charts": [
                        {
                            "title": "Exception count",
                            "instrument": "process.runtime.dotnet.exceptions.count",
                            "required": false, // default
                            "resource": null,  // default
                            "kind": "Graph"    // upper-case
                        },
                        {
                            "title": "Assembly count",
                            "instrument": "process.runtime.dotnet.assemblies.count",
                            "kind": "table"    // lower-case
                        },
                        {
                            "title": "Thread Pool Completion",
                            "instrument": "process.runtime.dotnet.thread_pool.completed_items.count",
                            "required": true   // non-default
                        }
                    ]
                },
                {
                    "name": "Envoy",
                    "priority": 99,
                    "charts": [
                        {
                            "title": "Connection count",
                            "instrument": "envoy.connection.count",
                            "required": true,   // non-default
                            "resource": "envoy" // non-default
                        }
                    ]
                }
            ]
            """;

        var dashpages = DashpageJsonFilePersistence.Deserialize(json);

        Assert.Collection(dashpages,
            dashpage =>
            {
                Assert.Equal(".NET", dashpage.Name);
                Assert.Equal(99, dashpage.Priority);
                Assert.Collection(dashpage.Charts,
                    chart =>
                    {
                        Assert.Equal("Exception count", chart.Title);
                        Assert.Equal("process.runtime.dotnet.exceptions.count", chart.InstrumentName);
                        Assert.False(chart.IsRequired);
                        Assert.Null(chart.ResourceName);
                        Assert.Equal(MetricViewKind.Graph, chart.DefaultViewKind);
                    },
                    chart =>
                    {
                        Assert.Equal("Assembly count", chart.Title);
                        Assert.Equal("process.runtime.dotnet.assemblies.count", chart.InstrumentName);
                        Assert.False(chart.IsRequired);
                        Assert.Null(chart.ResourceName);
                        Assert.Equal(MetricViewKind.Table, chart.DefaultViewKind);
                    },
                    chart =>
                    {
                        Assert.Equal("Thread Pool Completion", chart.Title);
                        Assert.Equal("process.runtime.dotnet.thread_pool.completed_items.count", chart.InstrumentName);
                        Assert.True(chart.IsRequired);
                        Assert.Null(chart.ResourceName);
                        Assert.Equal(MetricViewKind.Graph, chart.DefaultViewKind);
                    });
            },
            dashpage =>
            {
                Assert.Equal("Envoy", dashpage.Name);
                Assert.Equal(99, dashpage.Priority);
                Assert.Collection(dashpage.Charts,
                    chart =>
                    {
                        Assert.Equal("Connection count", chart.Title);
                        Assert.Equal("envoy.connection.count", chart.InstrumentName);
                        Assert.True(chart.IsRequired);
                        Assert.Equal("envoy", chart.ResourceName);
                        Assert.Equal(MetricViewKind.Graph, chart.DefaultViewKind);
                    });
            });
    }
}
