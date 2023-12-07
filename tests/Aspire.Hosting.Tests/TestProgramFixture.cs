// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests;

/// <summary>
/// This fixture ensures the TestProgram application is started before a test is executed.
/// </summary>
public abstract class TestProgramFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    private TestProgram? _testProgram;
    private HttpClient? _httpClient;

    public TestProgram TestProgram => _testProgram!;

    public DistributedApplication App => _app!;

    public HttpClient HttpClient => _httpClient!;

    public abstract TestProgram CreateTestProgram();

    public abstract Task WaitReadyStateAsync(CancellationToken cancellationToken = default);

    public async Task InitializeAsync()
    {
        _testProgram = CreateTestProgram();

        _app = _testProgram.Build();

        _httpClient = _app.Services.GetRequiredService<IHttpClientFactory>().CreateClient();

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        await _app.StartAsync(cts.Token);

        await WaitReadyStateAsync(cts.Token);
    }

    public async Task DisposeAsync()
    {
        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }

        if (_httpClient != null)
        {
            _httpClient!.Dispose();
        }
    }
}

public class IntegrationServicesFixture : TestProgramFixture
{
    public override TestProgram CreateTestProgram()
    {
        var testProgram = TestProgram.Create<DistributedApplicationTests>(includeIntegrationServices: true);

        testProgram.AppBuilder.Services
            .AddHttpClient()
            .ConfigureHttpClientDefaults(b =>
            {
                b.UseSocketsHttpHandler((handler, sp) => handler.PooledConnectionLifetime = TimeSpan.FromSeconds(5));
            });

        return testProgram;
    }

    public override Task WaitReadyStateAsync(CancellationToken cancellationToken = default)
    {
        return TestProgram.IntegrationServiceABuilder!.HttpGetPidAsync(HttpClient, "http", cancellationToken);
    }
}

public class NodeJsFixture : TestProgramFixture
{
    public override TestProgram CreateTestProgram()
    {
        var testProgram = TestProgram.Create<DistributedApplicationTests>(includeNodeApp: true);

        testProgram.AppBuilder.Services
            .AddHttpClient()
            .ConfigureHttpClientDefaults(b =>
            {
                b.UseSocketsHttpHandler((handler, sp) => handler.PooledConnectionLifetime = TimeSpan.FromSeconds(5));
                b.AddStandardResilienceHandler();
            });

        return testProgram;
    }

    public override Task WaitReadyStateAsync(CancellationToken cancellationToken = default)
    {
        return TestProgram.NodeAppBuilder!.HttpGetStringWithRetryAsync(HttpClient, "http", "/", cancellationToken);
    }
}

[CollectionDefinition("IntegrationServices")]
public class IntegrationServicesCollection : ICollectionFixture<IntegrationServicesFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

[CollectionDefinition("NodeJs")]
public class NodeJsCollection : ICollectionFixture<NodeJsFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
