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
    private readonly IDistributedApplicationBuilder _innerBuilder;
    private bool _builtApp;

    public ConfigurationManager Configuration => _innerBuilder.Configuration;
    public string AppHostDirectory => _innerBuilder.AppHostDirectory;
    public IHostEnvironment Environment => _innerBuilder.Environment;
    public IServiceCollection Services => _innerBuilder.Services;
    public DistributedApplicationExecutionContext ExecutionContext => _innerBuilder.ExecutionContext;
    public IResourceCollection Resources => _innerBuilder.Resources;

    public static TestDistrubtedApplicationBuilder Create() => new TestDistrubtedApplicationBuilder(DistributedApplication.CreateBuilder());

    private TestDistrubtedApplicationBuilder(IDistributedApplicationBuilder builder)
    {
        _innerBuilder = builder;
    }

    public IResourceBuilder<T> AddResource<T>(T resource) where T : IResource
    {
        return _innerBuilder.AddResource(resource);
    }

    public IResourceBuilder<T> CreateResourceBuilder<T>(T resource) where T : IResource
    {
        return _innerBuilder.CreateResourceBuilder(resource);
    }

    public DistributedApplication Build()
    {
        _builtApp = true;
        return _innerBuilder.Build();
    }

    public void Dispose()
    {
        // When the builder is disposed we build a host and then dispose it.
        // This cleans up unmanaged resources on the inner builder.
        if (!_builtApp)
        {
            try
            {
                _innerBuilder.Build().Dispose();
            }
            catch
            {
                // Ignore errors.
            }
        }
    }
}

