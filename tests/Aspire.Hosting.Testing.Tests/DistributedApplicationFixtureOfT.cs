using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Hosting.Testing.Tests;

public sealed class DistributedApplicationFixture<TEntryPoint> : DistributedApplicationFactory<TEntryPoint>, IAsyncLifetime where TEntryPoint : class
{
    protected override void OnBuilderCreating(DistributedApplicationOptions applicationOptions, HostApplicationBuilderSettings hostOptions)
    {
        base.OnBuilderCreating(applicationOptions, hostOptions);
    }

    protected override void OnBuilderCreated(DistributedApplicationBuilder applicationBuilder)
    {
        base.OnBuilderCreated(applicationBuilder);
    }

    protected override void OnBuilding(DistributedApplicationBuilder applicationBuilder)
    {
        base.OnBuilding(applicationBuilder);
    }

    protected override void OnBuilt(DistributedApplication application)
    {
        Application = application;
        base.OnBuilt(application);
    }

    public DistributedApplication Application { get; private set; } = null!;

    public async Task InitializeAsync() => await StartAsync();

    async Task IAsyncLifetime.DisposeAsync() => await DisposeAsync();
}
