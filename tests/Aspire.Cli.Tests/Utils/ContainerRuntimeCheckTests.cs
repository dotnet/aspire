// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Utils.EnvironmentChecker;

namespace Aspire.Cli.Tests.Utils;

public class ContainerRuntimeCheckTests
{
    [Theory]
    [InlineData("Docker version 20.10.17, build 100c701", "20.10.17")]
    [InlineData("Docker version 27.5.1, build 9f9e405", "27.5.1")]
    [InlineData("Docker version 24.0.5, build ced0996", "24.0.5")]
    [InlineData("podman version 4.3.1", "4.3.1")]
    [InlineData("podman version 5.0.0", "5.0.0")]
    [InlineData("podman version 3.4.4", "3.4.4")]
    [InlineData("Docker version 19.03.15, build 99e3ed8919", "19.03.15")]
    [InlineData("Docker version 20.10, build abc123", "20.10")]
    public void ParseVersionFromOutput_WithValidVersionString_ReturnsCorrectVersion(string input, string expectedVersion)
    {
        var result = ContainerRuntimeCheck.ParseVersionFromOutput(input);

        Assert.NotNull(result);
        Assert.Equal(Version.Parse(expectedVersion), result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Docker is not installed")]
    [InlineData("Command not found")]
    [InlineData("random text without version info")]
    public void ParseVersionFromOutput_WithInvalidInput_ReturnsNull(string? input)
    {
        var result = ContainerRuntimeCheck.ParseVersionFromOutput(input!);

        Assert.Null(result);
    }

    [Fact]
    public void ParseVersionFromOutput_WithDockerDesktopOutput_ReturnsVersion()
    {
        // Docker Desktop on Windows/Mac may have additional output
        var input = """
            Docker version 27.5.1, build 9f9e405
            
            """;

        var result = ContainerRuntimeCheck.ParseVersionFromOutput(input);

        Assert.NotNull(result);
        Assert.Equal(new Version(27, 5, 1), result);
    }

    [Fact]
    public void ParseVersionFromOutput_WithPodmanVerboseOutput_ReturnsVersion()
    {
        // Podman may have additional output on some systems
        var input = """
            podman version 4.3.1
            API Version: 4.3.1
            """;

        var result = ContainerRuntimeCheck.ParseVersionFromOutput(input);

        Assert.NotNull(result);
        Assert.Equal(new Version(4, 3, 1), result);
    }

    [Fact]
    public void MinimumDockerVersion_IsSetCorrectly()
    {
        Assert.Equal("20.10.0", ContainerRuntimeCheck.MinimumDockerVersion);
    }

    [Fact]
    public void MinimumPodmanVersion_IsSetCorrectly()
    {
        Assert.Equal("4.0.0", ContainerRuntimeCheck.MinimumPodmanVersion);
    }

    [Fact]
    public void MinimumDockerVersion_CanBeParsedAsVersion()
    {
        var canParse = Version.TryParse(ContainerRuntimeCheck.MinimumDockerVersion, out var version);

        Assert.True(canParse);
        Assert.NotNull(version);
        Assert.Equal(new Version(20, 10, 0), version);
    }

    [Fact]
    public void MinimumPodmanVersion_CanBeParsedAsVersion()
    {
        var canParse = Version.TryParse(ContainerRuntimeCheck.MinimumPodmanVersion, out var version);

        Assert.True(canParse);
        Assert.NotNull(version);
        Assert.Equal(new Version(4, 0, 0), version);
    }

    [Fact]
    public void ParseVersionFromOutput_WithCaseInsensitiveVersion_ReturnsVersion()
    {
        // Test case insensitivity
        var inputs = new[]
        {
            "Docker VERSION 27.5.1, build abc",
            "docker Version 27.5.1",
            "PODMAN VERSION 4.3.1",
        };

        foreach (var input in inputs)
        {
            var result = ContainerRuntimeCheck.ParseVersionFromOutput(input);
            Assert.NotNull(result);
        }
    }
}
