// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aspire.Hosting.Tests;

public class ResourceNotificationTests
{
    [Fact]
    public void InitialStateCanBeSpecified()
    {
        var builder = DistributedApplication.CreateBuilder();

        var custom = builder.AddResource(new CustomResource("myResource"))
            .WithEndpoint(name: "ep", scheme: "http", hostPort: 8080)
            .WithEnvironment("x", "1000")
            .WithInitialState(new()
            {
                ResourceType = "MyResource",
                Properties = [("A", "B")],
            });

        var annotation = custom.Resource.Annotations.OfType<ResourceSnapshotAnnotation>().SingleOrDefault();

        Assert.NotNull(annotation);

        var state = annotation.InitialSnapshot;

        Assert.Equal("MyResource", state.ResourceType);
        Assert.Empty(state.EnvironmentVariables);
        Assert.Collection(state.Properties, c =>
        {
            Assert.Equal("A", c.Key);
            Assert.Equal("B", c.Value);
        });
    }

    [Fact]
    public async Task ResourceUpdatesAreQueued()
    {
        var resource = new CustomResource("myResource");

        var notificationService = new ResourceNotificationService(new NullLogger<ResourceNotificationService>());

        async Task<List<ResourceEvent>> GetValuesAsync(CancellationToken cancellationToken)
        {
            var values = new List<ResourceEvent>();

            await foreach (var item in notificationService.WatchAsync().WithCancellation(cancellationToken))
            {
                values.Add(item);

                if (values.Count == 2)
                {
                    break;
                }
            }

            return values;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var enumerableTask = GetValuesAsync(cts.Token);

        await notificationService.PublishUpdateAsync(resource, state => state with { Properties = state.Properties.Add(("A", "value")) });

        await notificationService.PublishUpdateAsync(resource, state => state with { Properties = state.Properties.Add(("B", "value")) });

        var values = await enumerableTask;

        Assert.Collection(values,
            c =>
            {
                Assert.Equal(resource, c.Resource);
                Assert.Equal("myResource", c.ResourceId);
                Assert.Equal("CustomResource", c.Snapshot.ResourceType);
                Assert.Equal("value", c.Snapshot.Properties.Single(p => p.Key == "A").Value);
            },
            c =>
            {
                Assert.Equal(resource, c.Resource);
                Assert.Equal("myResource", c.ResourceId);
                Assert.Equal("CustomResource", c.Snapshot.ResourceType);
                Assert.Equal("value", c.Snapshot.Properties.Single(p => p.Key == "B").Value);
            });
    }

    [Fact]
    public async Task WatchingAllResourcesNotifiesOfAnyResourceChange()
    {
        var resource1 = new CustomResource("myResource1");
        var resource2 = new CustomResource("myResource2");

        var notificationService = new ResourceNotificationService(new NullLogger<ResourceNotificationService>());

        async Task<List<ResourceEvent>> GetValuesAsync(CancellationToken cancellation)
        {
            var values = new List<ResourceEvent>();

            await foreach (var item in notificationService.WatchAsync().WithCancellation(cancellation))
            {
                values.Add(item);

                if (values.Count == 3)
                {
                    break;
                }
            }

            return values;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var enumerableTask = GetValuesAsync(cts.Token);

        await notificationService.PublishUpdateAsync(resource1, state => state with { Properties = state.Properties.Add(("A", "value")) });

        await notificationService.PublishUpdateAsync(resource2, state => state with { Properties = state.Properties.Add(("B", "value")) });

        await notificationService.PublishUpdateAsync(resource1, "replica1", state => state with { Properties = state.Properties.Add(("C", "value")) });

        var values = await enumerableTask;

        Assert.Collection(values,
            c =>
            {
                Assert.Equal(resource1, c.Resource);
                Assert.Equal("myResource1", c.ResourceId);
                Assert.Equal("CustomResource", c.Snapshot.ResourceType);
                Assert.Equal("value", c.Snapshot.Properties.Single(p => p.Key == "A").Value);
            },
            c =>
            {
                Assert.Equal(resource2, c.Resource);
                Assert.Equal("myResource2", c.ResourceId);
                Assert.Equal("CustomResource", c.Snapshot.ResourceType);
                Assert.Equal("value", c.Snapshot.Properties.Single(p => p.Key == "B").Value);
            },
            c =>
            {
                Assert.Equal(resource1, c.Resource);
                Assert.Equal("replica1", c.ResourceId);
                Assert.Equal("CustomResource", c.Snapshot.ResourceType);
                Assert.Equal("value", c.Snapshot.Properties.Single(p => p.Key == "C").Value);
            });
    }

    private sealed class CustomResource(string name) : Resource(name),
        IResourceWithEnvironment,
        IResourceWithConnectionString
    {
        public ReferenceExpression ConnectionStringExpression =>
            ReferenceExpression.Create($"CustomConnectionString");
    }
}
