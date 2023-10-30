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

    public IResourceCollection Resources { get; } = new ResourceCollection();

    public DistributedApplicationBuilder(DistributedApplicationOptions options)
    {
        _args = options.Args ?? [];
        _innerBuilder = new HostApplicationBuilder();

        // Core things
        _innerBuilder.Services.AddSingleton(sp => new DistributedApplicationModel(Resources));
        _innerBuilder.Services.AddHostedService<DistributedApplicationRunner>();
        _innerBuilder.Services.AddSingleton(options);

        // DCP stuff
        _innerBuilder.Services.AddLifecycleHook<DcpDistributedApplicationLifecycleHook>();
        _innerBuilder.Services.AddSingleton<ApplicationExecutor>();
        _innerBuilder.Services.AddHostedService<DcpHostService>();

        // We need a unique path per application instance
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _innerBuilder.Services.AddSingleton(new Locations(path));
        _innerBuilder.Services.AddSingleton<KubernetesService>();

        // Publishing support
        ConfigurePublishingOptions(options);
        _innerBuilder.Services.AddLifecycleHook<AutomaticManifestPublisherBindingInjectionHook>();
        _innerBuilder.Services.AddLifecycleHook<Http2TransportMutationHook>();
        _innerBuilder.Services.AddKeyedSingleton<IDistributedApplicationPublisher, ManifestPublisher>("manifest");
        _innerBuilder.Services.AddKeyedSingleton<IDistributedApplicationPublisher, DcpPublisher>("dcp");
    }

    private void ConfigurePublishingOptions(DistributedApplicationOptions options)
    {
        var switchMappings = new Dictionary<string, string>()
        {
            { "--publisher", "Publishing:Publisher" },
            { "--output-path", "Publishing:OutputPath" },
            { "--dcp-cli-path", "DcpPublisher:CliPath" },
        };
        _innerBuilder.Configuration.AddCommandLine(options.Args ?? [], switchMappings);
        _innerBuilder.Services.Configure<PublishingOptions>(_innerBuilder.Configuration.GetSection(PublishingOptions.Publishing));
        _innerBuilder.Services.Configure<DcpOptions>(
            o => o.ApplyApplicationConfiguration(
                options,
                dcpPublisherConfiguration: _innerBuilder.Configuration.GetSection(DcpOptions.DcpPublisher),
                publishingConfiguration: _innerBuilder.Configuration.GetSection(PublishingOptions.Publishing)
            )
        );
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

    public IResourceBuilder<T> AddResource<T>(T resource) where T : IResource
    {
        if (Resources.FirstOrDefault(r => r.Name == resource.Name) is { } existingResource)
        {
            throw new DistributedApplicationException($"Cannot add resource of type '{resource.GetType()}' with name '{resource.Name}' because resource of type '{existingResource.GetType()}' with that name already exists.");
        }

        Resources.Add(resource);
        var builder = new DistributedApplicationResourceBuilder<T>(this, resource);
        return builder;
    }
}
