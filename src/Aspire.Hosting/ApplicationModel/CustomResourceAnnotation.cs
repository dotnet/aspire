// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading.Channels;
using Aspire.Dashboard.Model;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// The annotation that reflects how a resource shows up in the dashboard.
/// This is a single producer, single consumer channel model for pushing updates to the dashboard.
/// The resource server will be the only caller of WatchAsync.
/// </summary>
public sealed class CustomResourceAnnotation(Func<CustomResourceState> initialState) : IResourceAnnotation
{
    private readonly Channel<CustomResourceState> _channel = Channel.CreateUnbounded<CustomResourceState>();

    /// <summary>
    /// Watch for changes to the dashboard state for a resource.
    /// </summary>
    public IAsyncEnumerable<CustomResourceState> WatchAsync(CancellationToken cancellationToken = default) => _channel.Reader.ReadAllAsync(cancellationToken);

    /// <summary>
    /// Gets the initial snapshot of the dashboard state for this resource.
    /// </summary>
    public CustomResourceState GetInitialState() => initialState();

    /// <summary>
    /// Updates the snapshot of the dashboard state for a resource.
    /// </summary>
    /// <param name="state">The new <see cref="CustomResourceState"/>.</param>
    public async Task UpdateStateAsync(CustomResourceState state)
    {
        await _channel.Writer.WriteAsync(state).ConfigureAwait(false);
    }
}

/// <summary>
/// The context for a all of the properties and URLs that should show up in the dashboard for a resource.
/// </summary>
public record CustomResourceState
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
    public ImmutableArray<(string Name, string Value)> EnviromentVariables { get; init; } = [];

    /// <summary>
    /// The URLs that should show up in the dashboard for this resource.
    /// </summary>
    public ImmutableArray<string> Urls { get; init; } = [];

    /// <summary>
    /// Creates a new <see cref="CustomResourceState"/> for a resource using the well known annotations.
    /// </summary>
    /// <param name="resource">The resource.</param>
    /// <returns>The new <see cref="CustomResourceState"/>.</returns>
    public static CustomResourceState Create(IResource resource)
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
            var envContext = new EnvironmentCallbackContext(new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run));
            foreach (var annotation in environmentCallbacks)
            {
                annotation.Callback(envContext);
            }

            environmentVariables = [.. envContext.EnvironmentVariables.Select(e => (e.Key, e.Value))];
        }

        ImmutableArray<(string, string)> properties = [];
        if (resource is IResourceWithConnectionString connectionStringResource)
        {
            properties = [("ConnectionString", connectionStringResource.GetConnectionString() ?? "")];
        }

        // Initialize the state with the well known annotations
        return new CustomResourceState()
        {
            ResourceType = resource.GetType().Name.Replace("Resource", ""),
            EnviromentVariables = environmentVariables,
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
