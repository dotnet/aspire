// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting;

public interface IDistributedApplicationBuilder
{
    public ConfigurationManager Configuration { get; }
    public IHostEnvironment Environment { get; }
    public IServiceCollection Services { get; }
    public IDistributedApplicationResourceCollection Resources { get; }

    /// <summary>
    /// Adds a resource to the application.
    /// </summary>
    /// <returns></returns>
    IDistributedApplicationResourceBuilder<T> AddResource<T>(T resource) where T : IDistributedApplicationResource;

    DistributedApplication Build();
}
