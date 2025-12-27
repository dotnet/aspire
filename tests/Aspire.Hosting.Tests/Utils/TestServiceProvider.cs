// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Design;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Tests.Dcp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting.Tests.Utils;

public sealed class TestServiceProvider : IServiceProvider
{
    private readonly ServiceContainer _serviceContainer = new ServiceContainer();

    public TestServiceProvider(IConfiguration? configuration = null)
    {
        _serviceContainer.AddService(typeof(IDcpDependencyCheckService), new TestDcpDependencyCheckService());
        var config = configuration ?? new ConfigurationManager();
        _serviceContainer.AddService(typeof(IConfiguration), config);
        
        // Register AppHostEnvironment with the test configuration
        var hostEnvironment = new TestHostEnvironment();
        _serviceContainer.AddService(typeof(AppHostEnvironment), new AppHostEnvironment(config, hostEnvironment));
    }

    public object? GetService(Type serviceType)
    {
        return _serviceContainer.GetService(serviceType);
    }

    public TestServiceProvider AddService<TService>(TService instance)
        where TService : class
    {
        _serviceContainer.AddService(typeof(TService), instance);
        return this;
    }

    public TestServiceProvider AddService<TService>(ServiceCreatorCallback callback)
        where TService : class
    {
        _serviceContainer.AddService(typeof(TService), callback);
        return this;
    }

    public static IServiceProvider Instance { get; } = new TestServiceProvider();

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "TestApp";
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Directory.GetCurrentDirectory());
        public string ContentRootPath { get; set; } = "/test";
        public string EnvironmentName { get; set; } = "Development";
    }
}
