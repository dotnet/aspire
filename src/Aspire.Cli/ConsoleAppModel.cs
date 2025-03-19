// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;

namespace Aspire.Cli;

/// <summary>
/// A minimal representation of the AppModel to support rendering
/// information in the client.
/// </summary>
internal sealed class ConsoleAppModel
{
    private string? _dashboardLoginUrl;

    public string? DashboardLoginUrl
    {
        get => _dashboardLoginUrl;
        set
        {
            _dashboardLoginUrl = value;
            ModelUpdatedChannel.Writer.TryWrite(DateTimeOffset.UtcNow);
        }
    }

    private DateTimeOffset? _lastPing;

    public DateTimeOffset? LastPing
    {
        get => _lastPing;
        set
        {
            _lastPing = value;
            ModelUpdatedChannel.Writer.TryWrite(DateTimeOffset.UtcNow);
        }
    }

    public IEnumerable<ConsoleResource> Resources { get; } = new List<ConsoleResource>();

    public Channel<DateTimeOffset> ModelUpdatedChannel { get; } = Channel.CreateBounded<DateTimeOffset>(1);

    public void UpdateResource(string resourceName, string resourceType, string resourceStatus, string[]? resourceUris)
    {
        var resources = (List<ConsoleResource>)Resources;
        var resource = resources.FirstOrDefault(r => r.Name == resourceName);
        if (resource != null)
        {
            resource.State = resourceStatus;
            resource.Endpoints = resourceUris;
        }
        else
        {
            resources.Add(new ConsoleResource(resourceName, resourceType,  resourceStatus, resourceUris));
        }

        ModelUpdatedChannel.Writer.TryWrite(DateTimeOffset.UtcNow);
    }
}

internal sealed class ConsoleResource(string name, string type, string state, string[]? endpoints)
{
    public string Name { get; set; } = name;

    public string Type { get; set; } = type;

    public string State { get; set; } = state;

    public string[]? Endpoints { get; set; } = endpoints;
}
