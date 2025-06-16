// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Rendering;

internal class RunExperimentalState : RenderableState
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

    public async Task UpdateResourceAsync(CliResource resource, CancellationToken cancellationToken)
    {
        if (_cliResources.Any(r => r.Name == resource.Name))
        {
            // Update existing resource
            var existingResource = _cliResources.First(r => r.Name == resource.Name);
            existingResource.Name = resource.Name; // Update properties as needed
            // Add more properties to update if necessary
        }
        else
        {
            // Add new resource
            _cliResources.Add(resource);
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
    public required string Name { get; set; }
}