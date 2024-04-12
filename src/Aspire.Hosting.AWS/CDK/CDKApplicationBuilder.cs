using Amazon.CDK;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using IResource = Aspire.Hosting.ApplicationModel.IResource;

namespace Aspire.Hosting.AWS.CDK;

internal sealed class CDKApplicationBuilder : ICDKApplicationBuilder
{
    private readonly IDistributedApplicationBuilder _innerBuilder;

    public CDKApplicationBuilder(IDistributedApplicationBuilder builder)
    {
        _innerBuilder = builder;
        _innerBuilder.Services
            .AddSingleton<CDKApplicationExecutionContext>(_ => new CDKApplicationExecutionContext(App))
            .TryAddLifecycleHook<CDKProvisioner>();
    }

    public App App { get; } = new();

    public IResourceBuilder<T> AddResource<T>(T resource) where T : IResource
        => _innerBuilder.AddResource(resource);

    public IResourceBuilder<T> CreateResourceBuilder<T>(T resource) where T : IResource
        => _innerBuilder.CreateResourceBuilder(resource);

    public DistributedApplication Build()
        => _innerBuilder.Build();

    public ConfigurationManager Configuration => _innerBuilder.Configuration;
    public string AppHostDirectory => _innerBuilder.AppHostDirectory;
    public IHostEnvironment Environment => _innerBuilder.Environment;
    public IServiceCollection Services => _innerBuilder.Services;
    public DistributedApplicationExecutionContext ExecutionContext => _innerBuilder.ExecutionContext;
    public IResourceCollection Resources => _innerBuilder.Resources;
}
