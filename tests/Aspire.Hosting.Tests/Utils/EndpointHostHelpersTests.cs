// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Tests.Utils;

public class EndpointHostHelpersTests
{
    [Theory]
    [InlineData("localhost", true)]
    [InlineData("LOCALHOST", true)]
    [InlineData("LocalHost", true)]
    [InlineData("LoCaLhOsT", true)]
    [InlineData("app.localhost", false)]
    [InlineData("api.localhost", false)]
    [InlineData("127.0.0.1", false)]
    [InlineData("::1", false)]
    [InlineData("example.com", false)]
    [InlineData("notlocalhost", false)]
    [InlineData("localhostx", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsLocalhost_VariousInputs_ReturnsExpectedResult(string? host, bool expected)
    {
        // Act
        var result = EndpointHostHelpers.IsLocalhost(host);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("localhost", false)]
    [InlineData("app.localhost", true)]
    [InlineData("api.localhost", true)]
    [InlineData("my-service.localhost", true)]
    [InlineData("APP.LOCALHOST", true)]
    [InlineData("Api.LocalHost", true)]
    [InlineData("my-service.LOCALHOST", true)]
    [InlineData("a.b.c.localhost", true)]
    [InlineData("127.0.0.1", false)]
    [InlineData("::1", false)]
    [InlineData("example.com", false)]
    [InlineData("localhost.example.com", false)]
    [InlineData("notlocalhost", false)]
    [InlineData("localhostx", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsLocalhostTld_VariousInputs_ReturnsExpectedResult(string? host, bool expected)
    {
        // Act
        var result = EndpointHostHelpers.IsLocalhostTld(host);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("localhost", true)]
    [InlineData("LOCALHOST", true)]
    [InlineData("LocalHost", true)]
    [InlineData("LoCaLhOsT", true)]
    [InlineData("app.localhost", true)]
    [InlineData("api.localhost", true)]
    [InlineData("my-service.localhost", true)]
    [InlineData("APP.LOCALHOST", true)]
    [InlineData("Api.LocalHost", true)]
    [InlineData("my-service.LOCALHOST", true)]
    [InlineData("a.b.c.localhost", true)]
    [InlineData("127.0.0.1", false)]
    [InlineData("::1", false)]
    [InlineData("example.com", false)]
    [InlineData("localhost.example.com", false)]
    [InlineData("notlocalhost", false)]
    [InlineData("localhostx", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsLocalhostOrLocalhostTld_VariousInputs_ReturnsExpectedResult(string? host, bool expected)
    {
        // Act
        var result = EndpointHostHelpers.IsLocalhostOrLocalhostTld(host);

        // Assert
        Assert.Equal(expected, result);
    }
}
