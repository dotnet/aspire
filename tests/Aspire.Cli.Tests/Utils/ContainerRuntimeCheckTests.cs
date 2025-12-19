// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Utils.EnvironmentChecker;

namespace Aspire.Cli.Tests.Utils;

public class ContainerRuntimeCheckTests
{
    #region ParseVersionFromJsonOutput Tests

    [Fact]
    public void ParseVersionFromJsonOutput_WithDockerJsonOutput_ReturnsVersion()
    {
        // Real Docker version -f json output
        var input = """{"Client":{"Platform":{"Name":"Docker Engine - Community"},"Version":"28.0.4","ApiVersion":"1.48","DefaultAPIVersion":"1.48","GitCommit":"b8034c0","GoVersion":"go1.23.7","Os":"linux","Arch":"amd64","BuildTime":"Tue Mar 25 15:07:16 2025","Context":"default"},"Server":{"Platform":{"Name":"Docker Engine - Community"},"Components":[{"Name":"Engine","Version":"28.0.4"}],"Version":"28.0.4","ApiVersion":"1.48"}}""";

        var result = ContainerRuntimeCheck.ParseVersionFromJsonOutput(input);

        Assert.NotNull(result);
        Assert.Equal(new Version(28, 0, 4), result);
    }

    [Fact]
    public void ParseVersionFromJsonOutput_WithPodmanJsonOutput_ReturnsVersion()
    {
        // Real Podman version -f json output
        var input = """{"Client":{"APIVersion":"4.9.3","Version":"4.9.3","GoVersion":"go1.22.2","GitCommit":"","BuiltTime":"Thu Jan  1 00:00:00 1970","Built":0,"OsArch":"linux/amd64","Os":"linux"}}""";

        var result = ContainerRuntimeCheck.ParseVersionFromJsonOutput(input);

        Assert.NotNull(result);
        Assert.Equal(new Version(4, 9, 3), result);
    }

    [Fact]
    public void ParseVersionFromJsonOutput_WithDockerDesktopWindowsJsonOutput_ReturnsVersion()
    {
        // Docker Desktop on Windows may have Server:null if daemon is not running
        var input = """{"Client":{"Version":"29.1.3","ApiVersion":"1.52","DefaultAPIVersion":"1.52","GitCommit":"f52814d","GoVersion":"go1.25.5","Os":"windows","Arch":"amd64","BuildTime":"Fri Dec 12 14:51:52 2025","Context":"desktop-linux"},"Server":null}""";

        var result = ContainerRuntimeCheck.ParseVersionFromJsonOutput(input);

        Assert.NotNull(result);
        Assert.Equal(new Version(29, 1, 3), result);
    }

    [Fact]
    public void ParseVersionFromJsonOutput_WithOldDockerVersion_ReturnsVersion()
    {
        var input = """{"Client":{"Version":"19.03.15","ApiVersion":"1.40"},"Server":null}""";

        var result = ContainerRuntimeCheck.ParseVersionFromJsonOutput(input);

        Assert.NotNull(result);
        Assert.Equal(new Version(19, 3, 15), result);
    }

    [Fact]
    public void ParseVersionFromJsonOutput_WithTwoPartVersion_ReturnsVersion()
    {
        var input = """{"Client":{"Version":"20.10","ApiVersion":"1.41"}}""";

        var result = ContainerRuntimeCheck.ParseVersionFromJsonOutput(input);

        Assert.NotNull(result);
        Assert.Equal(new Version(20, 10), result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("""{"Client":{}}""")]
    [InlineData("""{"Server":{"Version":"1.0.0"}}""")]
    public void ParseVersionFromJsonOutput_WithInvalidInput_ReturnsNull(string? input)
    {
        var result = ContainerRuntimeCheck.ParseVersionFromJsonOutput(input!);

        Assert.Null(result);
    }

    [Fact]
    public void ParseVersionFromJsonOutput_WithInvalidVersionString_ReturnsNull()
    {
        var input = """{"Client":{"Version":"not-a-version"}}""";

        var result = ContainerRuntimeCheck.ParseVersionFromJsonOutput(input);

        Assert.Null(result);
    }

    [Fact]
    public void ParseVersionFromJsonOutput_WithMalformedJson_ReturnsNull()
    {
        var input = "{\"Client\":{\"Version\":\"28.0.4\""; // Missing closing braces

        var result = ContainerRuntimeCheck.ParseVersionFromJsonOutput(input);

        Assert.Null(result);
    }

    #endregion

    #region ParseVersionFromOutput Tests (Text Fallback)

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
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Docker is not installed")]
    [InlineData("Command not found")]
    [InlineData("random text without version info")]
    public void ParseVersionFromOutput_WithInvalidInput_ReturnsNull(string input)
    {
        var result = ContainerRuntimeCheck.ParseVersionFromOutput(input);

        Assert.Null(result);
    }

    [Fact]
    public void ParseVersionFromOutput_WithNullInput_ReturnsNull()
    {
        var result = ContainerRuntimeCheck.ParseVersionFromOutput(null!);

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

    #endregion

    #region Minimum Version Constants Tests

    [Fact]
    public void MinimumDockerVersion_IsSetCorrectly()
    {
        Assert.Equal("28.0.0", ContainerRuntimeCheck.MinimumDockerVersion);
    }

    [Fact]
    public void MinimumPodmanVersion_IsSetCorrectly()
    {
        Assert.Equal("5.0.0", ContainerRuntimeCheck.MinimumPodmanVersion);
    }

    [Fact]
    public void MinimumDockerVersion_CanBeParsedAsVersion()
    {
        var canParse = Version.TryParse(ContainerRuntimeCheck.MinimumDockerVersion, out var version);

        Assert.True(canParse);
        Assert.NotNull(version);
        Assert.Equal(new Version(28, 0, 0), version);
    }

    [Fact]
    public void MinimumPodmanVersion_CanBeParsedAsVersion()
    {
        var canParse = Version.TryParse(ContainerRuntimeCheck.MinimumPodmanVersion, out var version);

        Assert.True(canParse);
        Assert.NotNull(version);
        Assert.Equal(new Version(5, 0, 0), version);
    }

    #endregion
}
