// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Qdrant;
using Aspire.Hosting.Utils;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace Aspire.Qdrant.Client.Tests;

public sealed class QdrantContainerFixture : IAsyncLifetime
{
    public IContainer? Container { get; private set; }
    private string _apiKey = string.Empty;
    public string GetConnectionString()
    {
        if (Container is null)
        {
            throw new InvalidOperationException("The test container was not initialized.");
        }
        var endpoint = new UriBuilder("http", Container.Hostname, Container.GetMappedPublicPort(6333)).ToString();
        return $"Endpoint={endpoint};Key={_apiKey}";
    }

    public async Task InitializeAsync()
    {
        if (RequiresDockerAttribute.IsSupported)
        {

            _apiKey = PasswordGenerator.Generate(minLength: 16, lower: true, upper: true, numeric: true, special: false, minLower: 1, minUpper: 1, minNumeric: 1, minSpecial: 0);

            Container = new ContainerBuilder()
              .WithImage($"{ComponentTestConstants.AspireTestContainerRegistry}/{QdrantContainerImageTags.Image}:{QdrantContainerImageTags.Tag}")
              .WithPortBinding(6333, true)
              .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(6333)))
              .WithEnvironment("QDRANT__SERVICE__API_KEY", _apiKey)
              .Build();

            await Container.StartAsync();
        }
    }

    public async Task DisposeAsync()
    {
        if (Container is not null)
        {
            await Container.DisposeAsync();
        }
    }
}
