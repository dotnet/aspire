// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Meilisearch;
using Aspire.Hosting.Utils;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace Aspire.Meilisearch.Tests;

public sealed class MeilisearchContainerFixture : IAsyncLifetime
{
    public IContainer? Container { get; private set; }
    private string _masterKey = string.Empty;
    public string GetConnectionString()
    {
        if (Container is null)
        {
            throw new InvalidOperationException("The test container was not initialized.");
        }
        var endpoint = new UriBuilder("http", Container.Hostname, Container.GetMappedPublicPort(7700)).ToString();
        return $"Endpoint={endpoint};MasterKey={_masterKey}";
    }

    public async Task InitializeAsync()
    {
        if (RequiresDockerAttribute.IsSupported)
        {

            //The master key must be at least 16-bytes-long and composed of valid UTF-8 characters.
            _masterKey = PasswordGenerator.Generate(minLength: 16, lower: true, upper: true, numeric: true, special: false, minLower: 1, minUpper: 1, minNumeric: 1, minSpecial: 0);

            Container = new ContainerBuilder()
              .WithImage($"{MeilisearchContainerImageTags.Registry}/{MeilisearchContainerImageTags.Image}:{MeilisearchContainerImageTags.Tag}")
              .WithPortBinding(7700, true)
              .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(7700)))
              .WithEnvironment("MEILI_MASTER_KEY", _masterKey)
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
