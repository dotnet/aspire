// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Elasticsearch;
using Testcontainers.Elasticsearch;
using Xunit;

namespace Aspire.Elastic.Clients.Elasticsearch.Tests;

public sealed class ElasticsearchContainerFixture : IAsyncLifetime
{
    public ElasticsearchContainer? Container { get; private set; }

    public string GetConnectionString() => Container?.GetConnectionString() ??
        throw new InvalidOperationException("The test container was not initialized.");

    public async Task InitializeAsync()
    {
        if (RequiresDockerAttribute.IsSupported)
        {
            Container = new ElasticsearchBuilder()
                .WithImage($"{TestConstants.AspireTestContainerRegistry}/{ElasticsearchContainerImageTags.Image}:{ElasticsearchContainerImageTags.Tag}")
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
