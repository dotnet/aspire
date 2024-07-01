// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Testing;
using Xunit;

namespace Aspire.Hosting.Tests;

/// <summary>
/// This fixture ensures the TestProgram application is started before a test is executed.
/// </summary>
public abstract class TestProgramFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    private TestProgram? _testProgram;

    public TestProgram TestProgram => _testProgram ?? throw new InvalidOperationException("TestProgram is not initialized.");

    public DistributedApplication App => _app ?? throw new InvalidOperationException("DistributedApplication is not initialized.");

    public abstract TestProgram CreateTestProgram();

    public abstract Task WaitReadyStateAsync(CancellationToken cancellationToken = default);

    public async Task InitializeAsync()
    {
        _testProgram = CreateTestProgram();

        _app = _testProgram.Build();

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

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

        _testProgram?.Dispose();
    }
}

/// <summary>
/// TestProgram with no dashboard, node app or integration services.
/// </summary>
/// <remarks>
/// Use <c>[Collection("SlimTestProgram")]</c> to inject this fixture in test constructors.
/// </remarks>
public class SlimTestProgramFixture : TestProgramFixture
{
    public override TestProgram CreateTestProgram()
    {
        return TestProgram.Create<DistributedApplicationTests>(randomizePorts: false);
    }

    public override async Task WaitReadyStateAsync(CancellationToken cancellationToken = default)
    {
        // Make sure services A, B and C are running
        using var clientA = App.CreateHttpClient(TestProgram.ServiceABuilder.Resource.Name, "http");
        await clientA.GetStringAsync("/", cancellationToken);

        using var clientB = App.CreateHttpClient(TestProgram.ServiceBBuilder.Resource.Name, "http");
        await clientB.GetStringAsync("/", cancellationToken);

        using var clientC = App.CreateHttpClient(TestProgram.ServiceCBuilder.Resource.Name, "http");
        await clientC.GetStringAsync("/", cancellationToken);
    }
}

/// <summary>
/// TestProgram with node app but no dashboard or integration services.
/// </summary>
/// <remarks>
/// Use <c>[Collection("NodeApp")]</c> to inject this fixture in test constructors.
/// </remarks>
public class NodeAppFixture : TestProgramFixture
{
    public override TestProgram CreateTestProgram() => TestProgram.Create<DistributedApplicationTests>(includeNodeApp: true, randomizePorts: false);

    public override async Task WaitReadyStateAsync(CancellationToken cancellationToken = default)
    {
        using var client = TestProgram.App!.CreateHttpClient(TestProgram.NodeAppBuilder!.Resource.Name, endpointName: "http");
        await client.GetStringAsync("/", cancellationToken);
    }
}

[CollectionDefinition("SlimTestProgram")]
public class SlimTestProgramCollection : ICollectionFixture<SlimTestProgramFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

[CollectionDefinition("NodeApp")]
public class NodeJsCollection : ICollectionFixture<NodeAppFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
