// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Backchannel;

namespace Aspire.Cli.Rendering;

internal class ConsoleDashboardState : RenderableState
{
    private readonly List<CliResource> _cliResources = new();

    public IEnumerable<CliResource> CliResources => _cliResources.AsReadOnly();

    private CliResource? _selectedResource;

    public CliResource? SelectedResource
    {
        get
        {
            if (_cliResources.Count == 0)
            {
                return null;
            }

            if (_selectedResource == null)
            {
                _selectedResource = _cliResources[0]; // Default to the first resource if none is selected
                return _selectedResource;
            }

            if (_cliResources.Contains(_selectedResource))
            {
                return _selectedResource;
            }
            else
            {
                return null;
            }
        }
    }

    public async Task SelectNextResourceAsync(CancellationToken cancellationToken)
    {
        var selectedResourceIndex = _cliResources.IndexOf(SelectedResource!);

        if (_cliResources.Count == 0)
        {
            return;
        }
        else if (selectedResourceIndex == _cliResources.Count - 1)
        {
            // If the last resource is selected, do nothing
            return;
        }
        else
        {
            _selectedResource = _cliResources[selectedResourceIndex + 1];
        }

        await Updated.Writer.WriteAsync(true, cancellationToken);
    }
    public async Task SelectPreviousResourceAsync(CancellationToken cancellationToken)
    {
        var selectedResourceIndex = _cliResources.IndexOf(SelectedResource!);

        if (selectedResourceIndex == 0)
        {
            return;
        }
        else
        {
            _selectedResource = _cliResources[selectedResourceIndex - 1];
        }

        await Updated.Writer.WriteAsync(true, cancellationToken);
    }

    public async Task UpdateResourceAsync(RpcResourceState resourceState, CancellationToken cancellationToken)
    {
        if (_cliResources.FirstOrDefault(r => r.ResourceName == resourceState.ResourceName && r.ResourceId == resourceState.ResourceId) is not { } resource)
        {
            // Update existing resource
            resource = new CliResource()
            {
                ResourceName = resourceState.ResourceName,
                ResourceId = resourceState.ResourceId,
                Type = resourceState.Type,
                State = resourceState.State,
                Endpoints = resourceState.Endpoints,
                Health = resourceState.Health
            };
            _cliResources.Add(resource);
        }
        else
        {
            resource.Type = resourceState.Type;
            resource.State = resourceState.State;
            resource.Endpoints = resourceState.Endpoints;
            resource.Health = resourceState.Health;
        }

        await Updated.Writer.WriteAsync(true, cancellationToken);
    }

    public string? StatusMessage { get; set; }

    public async Task UpdateStatusAsync(string message, CancellationToken cancellationToken)
    {
        StatusMessage = message;
        await Updated.Writer.WriteAsync(true, cancellationToken);
    }
}

internal class CliResource
{
    public required string ResourceName { get; set; }
    public required string ResourceId { get; set; }
    public required string Type { get; set; }
    public required string State { get; set; }
    public required string[] Endpoints { get; set; }
    public required string? Health { get; set; }
}