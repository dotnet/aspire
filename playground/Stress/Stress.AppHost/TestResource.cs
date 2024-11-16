// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Model;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;

static class TestResourceExtensions
{
    public static IResourceBuilder<TestResource> AddTestResource(this IDistributedApplicationBuilder builder, string name)
    {
        builder.Services.TryAddLifecycleHook<TestResourceLifecycleHook>();

        var rb = builder.AddResource(new TestResource(name))
                      .WithInitialState(new()
                      {
                          ResourceType = "Test Resource",
                          State = "Starting",
                          Properties = [
                              new("P1", "P2"),
                              new(CustomResourceKnownProperties.Source, "Custom")
                          ]
                      })
                      .ExcludeFromManifest();

        return rb;
    }

    public static IResourceBuilder<TestNestedResource> AddNestedResource(this IDistributedApplicationBuilder builder, string name, IResource parent)
    {
        var rb = builder.AddResource(new TestNestedResource(name, parent))
                      .WithInitialState(new()
                      {
                          ResourceType = "Test Nested Resource",
                          State = "Starting",
                          Properties = [
                              new("P1", "P2"),
                              new(CustomResourceKnownProperties.Source, "Custom"),
                              new(KnownProperties.Resource.ParentName, parent.Name)
                          ]
                      })
                      .ExcludeFromManifest();

        return rb;
    }
}

internal sealed class TestResourceLifecycleHook(ResourceNotificationService notificationService, ResourceLoggerService loggerService) : IDistributedApplicationLifecycleHook, IAsyncDisposable
{
    private readonly CancellationTokenSource _tokenSource = new();

    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        foreach (var resource in appModel.Resources.OfType<TestResource>())
        {
            var states = new[] { "Starting", "Running", "Finished" };

            var logger = loggerService.GetLogger(resource);

            Task.Run(async () =>
            {
                var seconds = Random.Shared.Next(2, 12);

                logger.LogInformation("Starting test resource {ResourceName} with update interval {Interval} seconds", resource.Name, seconds);

                await notificationService.PublishUpdateAsync(resource, state => state with
                {
                    Properties = [.. state.Properties, new("Interval", seconds.ToString(CultureInfo.InvariantCulture))]
                });

                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(seconds));

                while (await timer.WaitForNextTickAsync(_tokenSource.Token))
                {
                    var randomState = states[Random.Shared.Next(0, states.Length)];

                    await notificationService.PublishUpdateAsync(resource, state => state with
                    {
                        State = randomState
                    });

                    logger.LogInformation("Test resource {ResourceName} is now in state {State}", resource.Name, randomState);
                }
            },
            cancellationToken);
        }

        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _tokenSource.Cancel();
        return default;
    }
}

sealed class TestResource(string name) : Resource(name)
{

}

sealed class TestNestedResource(string name, IResource parent) : Resource(name), IResourceWithParent
{
    public IResource Parent { get; } = parent;
}
