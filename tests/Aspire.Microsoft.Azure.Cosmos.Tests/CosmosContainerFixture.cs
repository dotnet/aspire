// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestUtilities;
using Aspire.Hosting.Azure.CosmosDB;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace Aspire.Microsoft.Azure.Cosmos.Tests;

public sealed class CosmosContainerFixture : IAsyncLifetime
{
    public IContainer? Container { get; private set; }

    public string GetConnectionString() => Container is not null
        ? $"AccountKey={CosmosConstants.EmulatorAccountKey};AccountEndpoint=https://{Container.Hostname}:{Container.GetMappedPublicPort(8081)};DisableServerCertificateValidation=True;"
        : throw new InvalidOperationException("The test container was not initialized.");

    public async ValueTask InitializeAsync()
    {
        if (RequiresDockerAttribute.IsSupported)
        {
            try
            {
                // Use a longer timeout for cosmos emulator since it's known to be slow
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
                Container = await CreateContainerAsync(cts.Token);
            }
            catch (Exception)
            {
                // Cosmos emulator is known to be flaky - if it fails to start, continue without it
                // Tests will fall back to using fake connection strings
                Container = null;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Container is not null)
        {
            await Container.DisposeAsync();
        }
    }

    public static async Task<IContainer> CreateContainerAsync(CancellationToken cancellationToken = default)
    {
        var container = new ContainerBuilder()
            .WithImage($"{CosmosDBEmulatorContainerImageTags.Registry}/{CosmosDBEmulatorContainerImageTags.Image}:{CosmosDBEmulatorContainerImageTags.Tag}")
            .WithPortBinding(8081, true)
            .WithEnvironment("AZURE_COSMOS_EMULATOR_PARTITION_COUNT", "10")
            .WithEnvironment("AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE", "false")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8081))
            .Build();

        await container.StartAsync(cancellationToken);

        return container;
    }
}