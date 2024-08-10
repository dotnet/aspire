// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Milvus.Client.Tests;
public class ConfigurationTests
{
    [Fact]
    public void EndpointIsNullByDefault()
    => Assert.Null(new MilvusClientSettings().Endpoint);

    [Fact]
    public void DatabaseIsNullByDefault()
    => Assert.Null(new MilvusClientSettings().Database);
}
