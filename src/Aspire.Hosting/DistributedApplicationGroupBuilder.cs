// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting;

internal sealed class DistributedApplicationGroupBuilder : IDistributedApplicationGroupBuilder
{
    private readonly List<IResourceBuilder<IResource>> _groupResourceBuilders = [];
    private readonly ResourceAnnotationCollection _annotations = [];

    public ConfigurationManager Configuration { get; }
    public string AppHostDirectory { get; }
    public Assembly? AppHostAssembly { get; }
    public IHostEnvironment Environment { get; }
    public IServiceCollection Services { get; }
    public IDistributedApplicationEventing Eventing { get; }
    public DistributedApplicationExecutionContext ExecutionContext { get; }
    public IResourceCollection Resources { get; }
    public ResourceGroupCollection Groups { get; }
    public IDistributedApplicationBuilder ApplicationBuilder { get; }

    public IDistributedApplicationGroupBuilder Resource => this;

    public DistributedApplicationGroupBuilder(IDistributedApplicationBuilder applicationBuilder, string name)
    {
        _annotations.Add(new ResourceGroupAnnotation { Name = name });

        Configuration = applicationBuilder.Configuration;
        AppHostDirectory = applicationBuilder.AppHostDirectory;
        AppHostAssembly = applicationBuilder.AppHostAssembly;
        Environment = applicationBuilder.Environment;
        Services = applicationBuilder.Services;
        Eventing = applicationBuilder.Eventing;
        ExecutionContext = applicationBuilder.ExecutionContext;
        Resources = applicationBuilder.Resources;
        Groups = [this];
        ApplicationBuilder = applicationBuilder;
    }

    public IResourceBuilder<T> AddResource<T>(T resource) where T : IResource
    {
        var builder = ApplicationBuilder.AddResource(resource);
        _groupResourceBuilders.Add((IResourceBuilder<IResource>)builder);
        return builder;
    }

    public IResourceBuilder<T> CreateResourceBuilder<T>(T resource) where T : IResource
    {
        return ApplicationBuilder.CreateResourceBuilder(resource);
    }

    public DistributedApplication Build() => throw new InvalidOperationException();

    DistributedApplication IDistributedApplicationBuilder.Build() => throw new InvalidOperationException();

    public void BuildGroup()
    {
        foreach (var resourceBuilder in _groupResourceBuilders)
        {
            foreach (var annotation in _annotations)
            {
                resourceBuilder.WithAnnotation(annotation);
            }
        }
    }
}
