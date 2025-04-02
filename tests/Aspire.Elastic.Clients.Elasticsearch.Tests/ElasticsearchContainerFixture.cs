// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestUtilities;
using Aspire.Hosting.Elasticsearch;
using Testcontainers.Elasticsearch;
using Xunit;
using Aspire.Components.Common.Tests;

namespace Aspire.Elastic.Clients.Elasticsearch.Tests;

public sealed class ElasticsearchContainerFixture : IAsyncLifetime
{
    public ElasticsearchContainer? Container { get; private set; }

    public string GetConnectionString() => Container?.GetConnectionString() ??
        throw new InvalidOperationException("The test container was not initialized.");

    public async ValueTask InitializeAsync()
    {
        if (RequiresDockerAttribute.IsSupported)
        {
            Container = new ElasticsearchBuilder()
                .WithImage($"{ComponentTestConstants.AspireTestContainerRegistry}/{ElasticsearchContainerImageTags.Image}:{ElasticsearchContainerImageTags.Tag}")
                .Build();
            await Container.StartAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Container is not null)
        {
            await Container.DisposeAsync();
        }
    }
}
