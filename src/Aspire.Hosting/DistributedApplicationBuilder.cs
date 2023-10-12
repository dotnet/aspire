// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting;

public class DistributedApplicationBuilder : IDistributedApplicationBuilder
{
    private readonly HostApplicationBuilder _innerBuilder;
    private readonly string[] _args;

    public IHostEnvironment Environment => _innerBuilder.Environment;

    public ConfigurationManager Configuration => _innerBuilder.Configuration;

    public IServiceCollection Services => _innerBuilder.Services;

    public IDistributedApplicationResourceCollection Resources { get; } = new DistributedApplicationResourceCollection();

    public DistributedApplicationBuilder(string[] args)
    {
        _args = args;
        _innerBuilder = new HostApplicationBuilder();

        // Core things
        _innerBuilder.Services.AddSingleton(sp => new DistributedApplicationModel(Resources));
        _innerBuilder.Services.AddHostedService<DistributedApplicationRunner>();

        // DCP stuff
        _innerBuilder.Services.AddLifecycleHook<DcpDistributedApplicationLifecycleHook>();
        _innerBuilder.Services.AddSingleton<ApplicationExecutor>();
        _innerBuilder.Services.AddHostedService<DcpHostService>();

        // Publishing support
        ConfigurePublishingOptions(args);
        _innerBuilder.Services.AddKeyedSingleton<IDistributedApplicationPublisher, ManifestPublisher>("manifest");
        _innerBuilder.Services.AddKeyedSingleton<IDistributedApplicationPublisher, DcpPublisher>("dcp");
    }

    private void ConfigurePublishingOptions(string[] args)
    {
        var switchMappings = new Dictionary<string, string>()
        {
            { "--publisher", "Publishing:Publisher" },
            { "--output-path", "Publishing:OutputPath" }
        };
        _innerBuilder.Configuration.AddCommandLine(args, switchMappings);
        _innerBuilder.Services.Configure<PublishingOptions>(_innerBuilder.Configuration.GetSection(PublishingOptions.Publishing));
    }

    public DistributedApplication Build()
    {
        AspireEventSource.Instance.DistributedApplicationBuildStart();
        try
        {
            var application = new DistributedApplication(_innerBuilder.Build(), _args);
            return application;
        }
        finally
        {
            AspireEventSource.Instance.DistributedApplicationBuildStop();
        }
        
    }

    public Dictionary<IDistributedApplicationResource, object> resourceBuilders = new();

    public IDistributedApplicationResourceBuilder<T> AddResource<T>(T resource) where T : IDistributedApplicationResource
    {
        // NOTE: This method is designed to be idempotent. Occasionally libraries will need to
        //       get access to a pre-existing builder that is wrapping a resource. We store
        //       references to all the builders we create so that if someone calls add resource
        //       on a resource that is already in the model we return the existing builder.
        if (resourceBuilders.TryGetValue(resource, out var existingBuilder))
        {
            return (IDistributedApplicationResourceBuilder<T>)existingBuilder;
        }
        else
        {
            Resources.Add(resource);
            var builder = new DistributedApplicationResourceBuilder<T>(this, resource);
            resourceBuilders.Add(resource, builder);
            return builder;
        }
    }
}
