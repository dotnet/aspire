// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace CatalogService.Tests;

using Aspire.Hosting;
using Aspire.Hosting.Postgres;
using Microsoft.Extensions.Hosting;

public sealed class CatalogScenarioFixture : IAsyncLifetime
{
    private readonly IDistributedApplicationResourceBuilder<PostgresContainerResource> _postgres;
    private readonly IHost _app;

    public CatalogScenarioFixture()
    {
        var appBuilder = DistributedApplication.CreateBuilder([]);
        _postgres = appBuilder.AddPostgresContainer("postgres");
        _app = appBuilder.Build();
    }

    public Dictionary<string, string?> GetConfiguration()
    {
        var config = new Dictionary<string, string?>
        {
            ["ConnectionStrings:catalog"] = _postgres.Resource.GetConnectionString()!
        };
        config.GetEnumerator();

        return config;
    }

    public async Task DisposeAsync()
    {
        await _app.StopAsync();
        if (_app is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
        else
        {
            _app.Dispose();
        }
    }

    public async Task InitializeAsync()
    {
        await _app.StartAsync();
    }
}
