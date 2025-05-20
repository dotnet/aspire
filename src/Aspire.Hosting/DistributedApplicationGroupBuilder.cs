// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

internal sealed class DistributedApplicationGroupBuilder(IDistributedApplicationBuilder applicationBuilder): IResourceBuilder<IResource>
{
    private readonly List<IResourceBuilder<IResource>> _groupResourceBuilders = new();
    private readonly List<Action<IResourceBuilder<IResource>>> _groupAnnotations = new();

    public IResourceBuilder<T> AddResource<T>(T resource) where T : IResource
    {
        var builder = applicationBuilder.AddResource(resource);
        _groupResourceBuilders.Add((IResourceBuilder<IResource>)builder);
        return builder;
    }

    public IDistributedApplicationBuilder ApplicationBuilder => applicationBuilder;
    public IResource Resource => throw new InvalidOperationException();

    public IResourceBuilder<IResource> WithAnnotation<TAnnotation>(TAnnotation annotation,
        ResourceAnnotationMutationBehavior behavior = ResourceAnnotationMutationBehavior.Append) where TAnnotation : IResourceAnnotation
    {
        _groupAnnotations.Add(resourceBuilder => resourceBuilder.WithAnnotation(annotation, behavior));
        return this;
    }

    public void Build()
    {
        foreach (var resourceBuilder in _groupResourceBuilders)
        {
            foreach (var annotation in _groupAnnotations)
            {
                annotation(resourceBuilder);
            }
        }
    }
}
