// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Hosting.Testing.Tests;

public class DistributedApplicationFixture<TEntryPoint> : DistributedApplicationFactory, IAsyncLifetime where TEntryPoint : class
{
    public DistributedApplicationFixture()
        : base(typeof(TEntryPoint), [])
    {
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

    public async ValueTask InitializeAsync() => await StartAsync();

    async ValueTask IAsyncDisposable.DisposeAsync() => await DisposeAsync();
}
