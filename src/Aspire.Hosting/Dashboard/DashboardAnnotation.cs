// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading.Channels;
using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dashboard;

/// <summary>
/// The annotation that reflects how a resource shows up in the dashboard.
/// This is a single producer, single consumer channel model for pushing updates to the dashboard.
/// The resource server will be the only caller of WatchAsync.
/// </summary>
public class DashboardAnnotation(Func<DashboardResourceState> initialState) : IResourceAnnotation
{
    private readonly Channel<DashboardResourceState> _channel = Channel.CreateUnbounded<DashboardResourceState>();

    /// <summary>
    /// Watch for changes to the dashboard state for a resource.
    /// </summary>
    public IAsyncEnumerable<DashboardResourceState> WatchAsync(CancellationToken cancellationToken = default) => _channel.Reader.ReadAllAsync(cancellationToken);

    /// <summary>
    /// Gets the initial snapshot of the dashboard state for this resource.
    /// </summary>
    public DashboardResourceState GetIntialState() => initialState();

    /// <summary>
    /// Updates the snapshot of the dashboard state for a resource.
    /// </summary>
    /// <param name="state">The new <see cref="DashboardResourceState"/>.</param>
    public async Task UpdateStateAsync(DashboardResourceState state)
    {
        await _channel.Writer.WriteAsync(state).ConfigureAwait(false);
    }
}

/// <summary>
/// The context for a all of the properties and URLs that should show up in the dashboard for a resource.
/// </summary>
public record DashboardResourceState
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
    /// Creates a new <see cref="DashboardResourceState"/> for a resource using the well known annotations.
    /// </summary>
    /// <param name="resource">The resource.</param>
    /// <returns>The new <see cref="DashboardResourceState"/>.</returns>
    public static DashboardResourceState Create(IResource resource)
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
        return new DashboardResourceState()
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
public static class DashboardKnownProperties
{
    /// <summary>
    /// The source of the resource
    /// </summary>
    public static string Source { get; } = KnownProperties.Resource.Source;
}
