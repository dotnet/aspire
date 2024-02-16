// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dashboard;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

/// <summary>
/// A builder for creating instances of <see cref="DistributedApplication"/>.
/// </summary>
public class DistributedApplicationBuilder : IDistributedApplicationBuilder
{
    private readonly HostApplicationBuilder _innerBuilder;
    private readonly string[] _args;

    /// <inheritdoc />
    public IHostEnvironment Environment => _innerBuilder.Environment;

    /// <inheritdoc />
    public ConfigurationManager Configuration => _innerBuilder.Configuration;

    /// <inheritdoc />
    public IServiceCollection Services => _innerBuilder.Services;

    /// <inheritdoc />
    public string AppHostDirectory { get; }

    /// <inheritdoc />
    public DistributedApplicationExecutionContext ExecutionContext { get; }

    /// <inheritdoc />
    public IResourceCollection Resources { get; } = new ResourceCollection();

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedApplicationBuilder"/> class with the specified options.
    /// </summary>
    /// <param name="options">The options for the distributed application.</param>
    public DistributedApplicationBuilder(DistributedApplicationOptions options)
    {
        _args = options.Args ?? [];
        _innerBuilder = new HostApplicationBuilder();

        _innerBuilder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);
        _innerBuilder.Logging.AddFilter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.None);

        AppHostDirectory = options.ProjectDirectory ?? _innerBuilder.Environment.ContentRootPath;

        // Make the app host directory available to the application via configuration
        _innerBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AppHost:Directory"] = AppHostDirectory
        });

        // Core things
        _innerBuilder.Services.AddSingleton(sp => new DistributedApplicationModel(Resources));
        _innerBuilder.Services.AddHostedService<DistributedApplicationLifecycle>();
        _innerBuilder.Services.AddHostedService<DistributedApplicationRunner>();
        _innerBuilder.Services.AddSingleton(options);

        // Dashboard
        _innerBuilder.Services.AddSingleton<DashboardServiceHost>();
        _innerBuilder.Services.AddHostedService<DashboardServiceHost>(sp => sp.GetRequiredService<DashboardServiceHost>());
        _innerBuilder.Services.AddLifecycleHook<DashboardManifestExclusionHook>();

        // DCP stuff
        _innerBuilder.Services.AddLifecycleHook<DcpDistributedApplicationLifecycleHook>();
        _innerBuilder.Services.AddSingleton<ApplicationExecutor>();
        _innerBuilder.Services.AddSingleton<IDashboardEndpointProvider, HostDashboardEndpointProvider>();
        _innerBuilder.Services.AddSingleton<IDashboardAvailability, HttpPingDashboardAvailability>();
        _innerBuilder.Services.AddSingleton<IDcpDependencyCheckService, DcpDependencyCheck>();
        _innerBuilder.Services.AddHostedService<DcpHostService>();

        // We need a unique path per application instance
        _innerBuilder.Services.AddSingleton(new Locations());
        _innerBuilder.Services.AddSingleton<IKubernetesService, KubernetesService>();

        // Publishing support
        ConfigurePublishingOptions(options);
        _innerBuilder.Services.AddLifecycleHook<AutomaticManifestPublisherBindingInjectionHook>();
        _innerBuilder.Services.AddLifecycleHook<Http2TransportMutationHook>();
        _innerBuilder.Services.AddKeyedSingleton<IDistributedApplicationPublisher, ManifestPublisher>("manifest");
        _innerBuilder.Services.AddKeyedSingleton<IDistributedApplicationPublisher, DcpPublisher>("dcp");

        ExecutionContext = _innerBuilder.Configuration["Publishing:Publisher"] switch
        {
            "manifest" => new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish),
            _ => new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run)
        };

        _innerBuilder.Services.AddSingleton<DistributedApplicationExecutionContext>(ExecutionContext);
    }

    private void ConfigurePublishingOptions(DistributedApplicationOptions options)
    {
        var switchMappings = new Dictionary<string, string>()
        {
            { "--publisher", "Publishing:Publisher" },
            { "--output-path", "Publishing:OutputPath" },
            { "--dcp-cli-path", "DcpPublisher:CliPath" },
            { "--container-runtime", "DcpPublisher:ContainerRuntime" },
            { "--dependency-check-timeout", "DcpPublisher:DependencyCheckTimeout" },
        };
        _innerBuilder.Configuration.AddCommandLine(options.Args ?? [], switchMappings);
        _innerBuilder.Services.Configure<PublishingOptions>(_innerBuilder.Configuration.GetSection(PublishingOptions.Publishing));
        _innerBuilder.Services.Configure<DcpOptions>(
            o => o.ApplyApplicationConfiguration(
                options,
                dcpPublisherConfiguration: _innerBuilder.Configuration.GetSection(DcpOptions.DcpPublisher),
                publishingConfiguration: _innerBuilder.Configuration.GetSection(PublishingOptions.Publishing),
                coreConfiguration: _innerBuilder.Configuration
            )
        );
    }

    /// <inheritdoc />
    public DistributedApplication Build()
    {
        AspireEventSource.Instance.DistributedApplicationBuildStart();
        try
        {
            // AddResource(resource) validates that a name is unique but it's possible to add resources directly to the resource collection.
            // Validate names for duplicates while building the application.
            foreach (var duplicateResourceName in Resources.GroupBy(r => r.Name, StringComparers.ResourceName)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key))
            {
                throw new DistributedApplicationException($"Multiple resources with the name '{duplicateResourceName}'. Resource names are case-insensitive.");
            }

            var application = new DistributedApplication(_innerBuilder.Build(), _args);
            return application;
        }
        finally
        {
            AspireEventSource.Instance.DistributedApplicationBuildStop();
        }
    }

    /// <inheritdoc />
    public IResourceBuilder<T> AddResource<T>(T resource) where T : IResource
    {
        if (Resources.FirstOrDefault(r => string.Equals(r.Name, resource.Name, StringComparisons.ResourceName)) is { } existingResource)
        {
            throw new DistributedApplicationException($"Cannot add resource of type '{resource.GetType()}' with name '{resource.Name}' because resource of type '{existingResource.GetType()}' with that name already exists. Resource names are case-insensitive.");
        }

        Resources.Add(resource);
        return CreateResourceBuilder(resource);
    }

    /// <inheritdoc />
    public IResourceBuilder<T> CreateResourceBuilder<T>(T resource) where T : IResource
    {
        var builder = new DistributedApplicationResourceBuilder<T>(this, resource);
        return builder;
    }
}
