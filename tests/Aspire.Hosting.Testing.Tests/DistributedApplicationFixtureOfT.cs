using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Hosting.Testing.Tests;

public sealed class DistributedApplicationFixture<TEntryPoint> : DistributedApplicationTestingHarness<TEntryPoint>, IAsyncLifetime where TEntryPoint : class
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

    public async Task InitializeAsync() => await base.InitializeAsync();

    async Task IAsyncLifetime.DisposeAsync() => await DisposeAsync();
}
