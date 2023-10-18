// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace CatalogService.Tests;

using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

public sealed class CatalogApiTests : IClassFixture<CatalogScenarioFixture>, IDisposable
{
    private readonly CatalogScenarioFixture _fixture;
    private readonly WebApplicationFactory<Program> _webApplicationFactory;
    private readonly HttpClient _httpClient;

    public CatalogApiTests(CatalogScenarioFixture fixture)
    {
        _fixture = fixture;
        _webApplicationFactory = new CustomWebApplicationFactory(fixture);
        _httpClient = _webApplicationFactory.CreateClient();
    }

    void IDisposable.Dispose()
    {
        _webApplicationFactory.Dispose();
    }

    [Fact]
    public async Task CatalogIsAlive()
    {
        var response = await _httpClient.GetAsync("liveness");
        response.EnsureSuccessStatusCode();
    }

    private sealed class CustomWebApplicationFactory(CatalogScenarioFixture fixture) : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureHostConfiguration(config =>
            {
                config.AddInMemoryCollection(fixture.GetConfiguration());
            });
            return base.CreateHost(builder);
        }
    }
}
