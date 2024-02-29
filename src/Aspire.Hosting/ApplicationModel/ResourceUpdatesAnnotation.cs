// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading.Channels;
using Aspire.Dashboard.Model;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// The annotation that allows publishing and subscribing to changes in the state of a resource.
/// </summary>
public sealed class ResourceUpdatesAnnotation(Func<ValueTask<CustomResourceSnapshot>> initialSnapshotFactory) : IResourceAnnotation
{
    private readonly CancellationTokenSource _streamClosedCts = new();

    private Action<CustomResourceSnapshot>? OnSnapshotUpdated { get; set; }

    /// <summary>
    /// Watch for changes to the dashboard state for a resource.
    /// </summary>
    public IAsyncEnumerable<CustomResourceSnapshot> WatchAsync() => new ResourceUpdatesAsyncEnumerable(this);

    /// <summary>
    /// Gets the initial snapshot of the dashboard state for this resource.
    /// </summary>
    public ValueTask<CustomResourceSnapshot> GetInitialSnapshotAsync() => initialSnapshotFactory();

    /// <summary>
    /// Updates the snapshot of the <see cref="CustomResourceSnapshot"/> for a resource.
    /// </summary>
    /// <param name="state">The new <see cref="CustomResourceSnapshot"/>.</param>
    public Task UpdateStateAsync(CustomResourceSnapshot state)
    {
        if (_streamClosedCts.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        OnSnapshotUpdated?.Invoke(state);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Signal that no more updates are expected for this resource.
    /// </summary>
    public void Complete()
    {
        _streamClosedCts.Cancel();
    }

    private sealed class ResourceUpdatesAsyncEnumerable(ResourceUpdatesAnnotation customResourceAnnotation) : IAsyncEnumerable<CustomResourceSnapshot>
    {
        public async IAsyncEnumerator<CustomResourceSnapshot> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            var channel = Channel.CreateUnbounded<CustomResourceSnapshot>();

            void WriteToChannel(CustomResourceSnapshot state)
                => channel.Writer.TryWrite(state);

            using var _ = customResourceAnnotation._streamClosedCts.Token.Register(() => channel.Writer.TryComplete());

            customResourceAnnotation.OnSnapshotUpdated = WriteToChannel;

            try
            {
                await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
                {
                    yield return item;
                }
            }
            finally
            {
                customResourceAnnotation.OnSnapshotUpdated -= WriteToChannel;

                channel.Writer.TryComplete();
            }
        }
    }
}

/// <summary>
/// An immutable snapshot of the state of a resource.
/// </summary>
public sealed record CustomResourceSnapshot
{
    /// <summary>
    /// The type of the resource.
    /// </summary>
    public required string ResourceType { get; init; }

    /// <summary>
    /// The properties that should show up in the dashboard for this resource.
    /// </summary>
    public required ImmutableArray<(string Key, string Value)> Properties { get; init; }

    /// <summary>
    /// Represents the state of the resource.
    /// </summary>
    public string? State { get; init; }

    /// <summary>
    /// The environment variables that should show up in the dashboard for this resource.
    /// </summary>
    public ImmutableArray<(string Name, string Value)> EnvironmentVariables { get; init; } = [];

    /// <summary>
    /// The URLs that should show up in the dashboard for this resource.
    /// </summary>
    public ImmutableArray<string> Urls { get; init; } = [];

    /// <summary>
    /// Creates a new <see cref="CustomResourceSnapshot"/> for a resource using the well known annotations.
    /// </summary>
    /// <param name="resource">The resource.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The new <see cref="CustomResourceSnapshot"/>.</returns>
    public static async ValueTask<CustomResourceSnapshot> CreateAsync(IResource resource, CancellationToken cancellationToken = default)
    {
        ImmutableArray<string> urls = [];

        if (resource.TryGetAnnotationsOfType<EndpointAnnotation>(out var endpointAnnotations))
        {
            static string GetUrl(EndpointAnnotation e) =>
                $"{e.UriScheme}://localhost:{e.Port}";

            urls = [.. endpointAnnotations.Where(e => e.Port is not null).Select(e => GetUrl(e))];
        }

        ImmutableArray<(string, string)> environmentVariables = [];

        if (resource.TryGetAnnotationsOfType<EnvironmentCallbackAnnotation>(out var environmentCallbacks))
        {
            var envContext = new EnvironmentCallbackContext(new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run), cancellationToken: cancellationToken);
            foreach (var annotation in environmentCallbacks)
            {
                await annotation.Callback(envContext).ConfigureAwait(false);
            }

            environmentVariables = [.. envContext.EnvironmentVariables.Select(e => (e.Key, e.Value))];
        }

        ImmutableArray<(string, string)> properties = [];
        if (resource is IResourceWithConnectionString connectionStringResource)
        {
            properties = [("ConnectionString", connectionStringResource.GetConnectionString() ?? "")];
        }

        // Initialize the state with the well known annotations
        return new CustomResourceSnapshot()
        {
            ResourceType = resource.GetType().Name.Replace("Resource", ""),
            EnvironmentVariables = environmentVariables,
            Urls = urls,
            Properties = properties
        };
    }
}

/// <summary>
/// Known properties for resources that show up in the dashboard.
/// </summary>
public static class CustomResourceKnownProperties
{
    /// <summary>
    /// The source of the resource
    /// </summary>
    public static string Source { get; } = KnownProperties.Resource.Source;
}
