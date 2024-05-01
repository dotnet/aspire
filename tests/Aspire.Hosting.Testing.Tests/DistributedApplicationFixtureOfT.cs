using Microsoft.Extensions.Hosting;
using Xunit;
using Xunit.Sdk;

namespace Aspire.Hosting.Testing.Tests;

public class DistributedApplicationFixture<TEntryPoint> : DistributedApplicationFactory, IAsyncLifetime where TEntryPoint : class
{
    public DistributedApplicationFixture()
        : base(typeof(TEntryPoint))
    {
        if (Environment.GetEnvironmentVariable("BUILD_BUILDID") != null)
        {
            throw new SkipException("These tests can only run in local environments.");
        }
    }

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
