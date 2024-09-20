// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Meilisearch.Tests;

public class ConfigurationTests
{
    [Fact]
    public void EndpointIsNullByDefault() =>
        Assert.Null(new MeilisearchClientSettings().Endpoint);

    [Fact]
    public void HealthChecksEnabledByDefault() =>
        Assert.False(new MeilisearchClientSettings().DisableHealthChecks);

    [Fact]
    public void MasterKeyIsNullByDefault() =>
      Assert.Null(new MeilisearchClientSettings().MasterKey);
}
