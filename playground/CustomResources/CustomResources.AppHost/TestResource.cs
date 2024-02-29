// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;

static class TestResourceExtensions
{
    public static IResourceBuilder<TestResource> AddTestResource(this IDistributedApplicationBuilder builder, string name)
    {
        builder.Services.AddLifecycleHook<TestResourceLifecycleHook>();

        var rb = builder.AddResource(new TestResource(name))
                      .WithResourceLogger()
                      .WithResourceUpdates(() => new()
                      {
                          ResourceType = "Test Resource",
                          State = "Starting",
                          Properties = [
                              ("P1", "P2"),
                              (CustomResourceKnownProperties.Source, "Custom")
                          ]
                      })
                      .ExcludeFromManifest();

        return rb;
    }
}

internal sealed class TestResourceLifecycleHook : IDistributedApplicationLifecycleHook, IAsyncDisposable
{
    private readonly CancellationTokenSource _tokenSource = new();

    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        foreach (var item in appModel.Resources.OfType<TestResource>())
        {
            if (item.TryGetLastAnnotation<ResourceUpdatesAnnotation>(out var resourceUpdates) &&
                item.TryGetLastAnnotation<ResourceLoggerAnnotation>(out var loggerAnnotation))
            {
                var states = new[] { "Starting", "Running", "Finished" };

                Task.Run(async () =>
                {
                    // Simulate custom resource state changes
                    var state = resourceUpdates.GetInitialSnapshot();
                    var seconds = Random.Shared.Next(2, 12);

                    state = state with
                    {
                        Properties = [.. state.Properties, ("Interval", seconds.ToString(CultureInfo.InvariantCulture))]
                    };

                    loggerAnnotation.Logger.LogInformation("Starting test resource {ResourceName} with update interval {Interval} seconds", item.Name, seconds);

                    // This might run before the dashboard is ready to receive updates, but it will be queued.
                    await resourceUpdates.UpdateStateAsync(state);

                    using var timer = new PeriodicTimer(TimeSpan.FromSeconds(seconds));

                    while (await timer.WaitForNextTickAsync(_tokenSource.Token))
                    {
                        var randomState = states[Random.Shared.Next(0, states.Length)];

                        state = state with
                        {
                            State = randomState
                        };

                        loggerAnnotation.Logger.LogInformation("Test resource {ResourceName} is now in state {State}", item.Name, randomState);

                        await resourceUpdates.UpdateStateAsync(state);
                    }
                },
                cancellationToken);
            }
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
