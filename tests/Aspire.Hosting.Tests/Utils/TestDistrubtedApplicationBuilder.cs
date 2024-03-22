// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting.Utils;

/// <summary>
/// DistributedApplication.CreateBuilder() creates a builder that includes configuration to read from appsettings.json.
/// The builder has a FileSystemWatcher, which can't be cleaned up unless a DistributedApplication is built and disposed.
/// This class wraps the builder and provides a way to automatically dispose it to prevent test failures from excessive
/// FileSystemWatcher instances from many tests.
/// </summary>
public sealed class TestDistrubtedApplicationBuilder : IDisposable, IDistributedApplicationBuilder
{
    private readonly IDistributedApplicationBuilder _builder;

    public ConfigurationManager Configuration => _builder.Configuration;
    public string AppHostDirectory => _builder.AppHostDirectory;
    public IHostEnvironment Environment => _builder.Environment;
    public IServiceCollection Services => _builder.Services;
    public DistributedApplicationExecutionContext ExecutionContext => _builder.ExecutionContext;
    public IResourceCollection Resources => _builder.Resources;

    public static TestDistrubtedApplicationBuilder Create() => new TestDistrubtedApplicationBuilder(DistributedApplication.CreateBuilder());

    private TestDistrubtedApplicationBuilder(IDistributedApplicationBuilder builder)
    {
        _builder = builder;
    }

    public IResourceBuilder<T> AddResource<T>(T resource) where T : IResource
    {
        return _builder.AddResource(resource);
    }

    public IResourceBuilder<T> CreateResourceBuilder<T>(T resource) where T : IResource
    {
        return _builder.CreateResourceBuilder(resource);
    }

    public DistributedApplication Build()
    {
        return _builder.Build();
    }

    public void Dispose()
    {
        try
        {
            _builder.Build().Dispose();
        }
        catch
        {
            // Ignore errors.
        }
    }
}

