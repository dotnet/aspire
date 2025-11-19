// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
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
    public List<string> PushImageCalls { get; } = [];

    public IEnumerable<ContainerImageOptions?> BuildImageOptions
    {
        get
        {
            foreach (var resource in BuildImageResources)
            {
                if (resource.TryGetLastAnnotation<ContainerImageOptionsCallbackAnnotation>(out var annotation))
                {
                    var context = new ContainerImageOptionsCallbackAnnotationContext
                    {
                        Resource = resource,
                        CancellationToken = default
                    };
                    yield return annotation.Callback(context).GetAwaiter().GetResult();
                }
                else
                {
                    yield return null;
                }
            }
        }
    }

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

    public Task PushImageAsync(string imageName, CancellationToken cancellationToken = default)
    {
        PushImageCalled = true;
        PushImageCalls.Add(imageName);
        return Task.CompletedTask;
    }
}
