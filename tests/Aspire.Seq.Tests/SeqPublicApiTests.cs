// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Seq;

public class SeqPublicApiTests
{
    [Fact]
    public void AddSeqEndpointShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string connectionName = "seq";

        var action = () => builder.AddSeqEndpoint(connectionName);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddSeqEndpointShouldThrowWhenConnectionNameIsNullOrEmpty(bool isNull)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var connectionName = isNull ? null! : string.Empty;

        var action = () => builder.AddSeqEndpoint(connectionName);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(connectionName), exception.ParamName);
    }

    // Temporary test failures for GitHub Checks validation
    [Fact]
    public void ArtificialFailureForTesting1()
    {
        // This test intentionally fails to validate GitHub Checks integration
        Assert.Fail("This test is intentionally failing to test GitHub Checks integration");
    }

    [Fact]
    public void ArtificialFailureForTesting2()
    {
        // This test intentionally fails to validate GitHub Checks integration
        Assert.Equal("expected", "actual");
    }
}
