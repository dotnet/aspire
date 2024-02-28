// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.Lifecycle;

static class TestResourceExtensions
{
    public static IResourceBuilder<TestResource> AddTestResource(this IDistributedApplicationBuilder builder, string name)
    {
        builder.Services.AddLifecycleHook<TestResourceLifecycleHook>();

        var rb = builder.AddResource(new TestResource(name))
                      .WithCustomResourceState(() => new()
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
            if (item.TryGetLastAnnotation<CustomResourceAnnotation>(out var annotation))
            {
                var states = new[] { "Starting", "Running", "Finished" };

                Task.Run(async () =>
                {
                    // Simulate custom resource state changes
                    var state = annotation.GetInitialState();
                    var seconds = Random.Shared.Next(2, 12);

                    state = state with
                    {
                        Properties = [.. state.Properties, ("Interval", seconds.ToString(CultureInfo.InvariantCulture))]
                    };

                    // This might run before the dashboard is ready to receive updates, but it will be queued.
                    await annotation.UpdateStateAsync(state);

                    using var timer = new PeriodicTimer(TimeSpan.FromSeconds(seconds));

                    while (await timer.WaitForNextTickAsync(_tokenSource.Token))
                    {
                        state = state with
                        {
                            State = states[Random.Shared.Next(0, states.Length)]
                        };

                        await annotation.UpdateStateAsync(state);
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
