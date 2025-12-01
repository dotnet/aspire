// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.Tests;

/// <summary>
/// Mock implementation of IResourceContainerImageBuilder for testing.
/// </summary>
public sealed class MockImageBuilder : IResourceContainerImageBuilder
{
    public bool BuildImageCalled { get; private set; }
    public bool BuildImagesCalled { get; private set; }
    public bool PushImageCalled { get; private set; }
    public List<IResource> BuildImageResources { get; } = [];
    public List<ContainerImageBuildOptions?> BuildImageOptions { get; } = [];
    public List<IResource> PushImageCalls { get; } = [];

    public Task BuildImageAsync(IResource resource, CancellationToken cancellationToken = default)
    {
        BuildImageCalled = true;
        BuildImageResources.Add(resource);
        return Task.CompletedTask;
    }

    public Task BuildImagesAsync(IEnumerable<IResource> resources, CancellationToken cancellationToken = default)
    {
        BuildImagesCalled = true;
        BuildImageResources.AddRange(resources);
        return Task.CompletedTask;
    }

    public Task PushImageAsync(IResource resource, CancellationToken cancellationToken)
    {
        PushImageCalled = true;
        PushImageCalls.Add(resource);
        return Task.CompletedTask;
    }
}
