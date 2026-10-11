// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Text.Json;
using Aspire.Hosting.Native.Server;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.Native.Core.Tests;

public class NativeConfigurationTests
{
    [Fact]
    public void ConfigurationProjectsCredentialsPathsAndCliMetadataOnce()
    {
        using var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ASPIRE_NATIVE_RPC_TOKEN"] = new('a', 64),
            ["ASPIRE_REMOTE_APPHOST_TOKEN"] = new('b', 64),
            ["ASPIRE_NATIVE_TRACE_OPERATIONS"] = "1",
            ["AppHost:FilePath"] = "file-path",
            ["ASPIRE_APPHOST_FILEPATH"] = "cli-file",
            ["AppHost:Path"] = "apphost-path",
            ["ASPIRE_NATIVE_APPHOST_PATH"] = "native-path",
            ["ASPIRE_NATIVE_DASHBOARD_PORT"] = "0",
            ["ASPIRE_NATIVE_DASHBOARD_API_KEY"] = "resource-key",
            ["AppHost:BrowserToken"] = "browser-key",
            ["AppHost:DashboardApiKey"] = "dashboard-key",
            ["ASPIRE_CLI_PID"] = "123",
            ["ASPIRE_CLI_STARTED"] = "1770000000",
            ["ASPIRE_CLI_STARTED_STABLE"] = "1770000000123",
            ["ASPIRE_CLI_LOG_FILE"] = "cli.log"
        });
        var snapshot = NativeServerConfiguration.Parse(configuration, launchedByCli: false);
        configuration["ASPIRE_NATIVE_RPC_TOKEN"] = "changed";
        configuration["AppHost:FilePath"] = "changed";

        Assert.Equal(new string('a', 64), snapshot.RpcToken);
        Assert.True(snapshot.TraceOperations);
        Assert.Equal("file-path", snapshot.AppHostPath);
        Assert.Equal(0, snapshot.DashboardPort);
        Assert.Equal("resource-key", snapshot.DashboardCredentials.ResourceServiceApiKey);
        Assert.Equal("browser-key", snapshot.DashboardCredentials.BrowserToken);
        Assert.Equal("dashboard-key", snapshot.DashboardCredentials.DashboardApiKey);
        Assert.Equal(nameof(NativeServerConfiguration), snapshot.ToString());
        Assert.Equal(nameof(NativeDashboardCredentials), snapshot.DashboardCredentials.ToString());
        Assert.Equal(123, snapshot.CliProcess.ProcessId);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1770000000), snapshot.CliProcess.StartedAt);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1770000000123), snapshot.CliProcess.StableStartedAt);
        Assert.Equal("cli.log", snapshot.CliProcess.LogFilePath);
    }

    [Fact]
    public void MissingCredentialsAreIndependentAndDefaultsRetainWireCompatibility()
    {
        using var configuration = new ConfigurationManager();
        configuration["ASPIRE_REMOTE_APPHOST_TOKEN"] = new('b', 64);
        var settings = NativeServerConfiguration.Parse(configuration, launchedByCli: false);
        Assert.Equal(new string('b', 64), settings.RpcToken);
        Assert.False(settings.TraceOperations);
        Assert.Null(settings.DashboardPort);
        Assert.Null(settings.AppHostPath);
        Assert.Equal("Native AppHost", settings.ApplicationName);
        var credentials = settings.DashboardCredentials;
        var values = new[] { credentials.ResourceServiceApiKey, credentials.BrowserToken, credentials.DashboardApiKey, credentials.OtlpApiKey };
        Assert.Equal(4, values.Distinct(StringComparer.Ordinal).Count());
        Assert.All(values, value => Assert.Equal(64, value.Length));
    }

    [Theory]
    [InlineData("ASPIRE_APPHOST_FILEPATH", "cli-file")]
    [InlineData("AppHost:Path", "apphost-path")]
    [InlineData("ASPIRE_NATIVE_APPHOST_PATH", "native-path")]
    public void AppHostFallbacksUseTheSamePriorityAsManagedHosting(string key, string expected)
    {
        using var configuration = new ConfigurationManager();
        configuration["ASPIRE_REMOTE_APPHOST_TOKEN"] = new('b', 64);
        configuration[key] = expected;
        Assert.Equal(expected, NativeServerConfiguration.Parse(configuration, launchedByCli: false).AppHostPath);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("True")]
    [InlineData("yes")]
    [InlineData("0")]
    public void TraceOptInAcceptsOnlyItsExistingLiteral(string value)
    {
        using var configuration = new ConfigurationManager();
        configuration["ASPIRE_REMOTE_APPHOST_TOKEN"] = new('b', 64);
        configuration["ASPIRE_NATIVE_TRACE_OPERATIONS"] = value;
        Assert.False(NativeServerConfiguration.Parse(configuration, launchedByCli: false).TraceOperations);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("65536")]
    [InlineData("not-a-port")]
    public void InvalidConfiguredPortsAreRejectedBeforeStartingServices(string value)
    {
        using var configuration = new ConfigurationManager();
        configuration["ASPIRE_REMOTE_APPHOST_TOKEN"] = new('b', 64);
        configuration["ASPIRE_NATIVE_DASHBOARD_PORT"] = value;
        Assert.Throws<ArgumentException>(() => NativeServerConfiguration.Parse(configuration, launchedByCli: false));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-time")]
    [InlineData("9223372036854775807")]
    [InlineData("-9223372036854775808")]
    public void MissingMalformedAndOutOfRangeOptionalProcessDatesRemainAbsent(string? value)
    {
        Assert.Null(NativeCliProcessMetadata.ParseTimestamp(value, milliseconds: false));
        Assert.Null(NativeCliProcessMetadata.ParseTimestamp(value, milliseconds: true));
    }

    [Fact]
    public void CliLaunchUsesSessionTokenInsteadOfInheritedNativeToken()
    {
        using var configuration = new ConfigurationManager();
        configuration["ASPIRE_NATIVE_RPC_TOKEN"] = "invalid-inherited-token";
        configuration["ASPIRE_REMOTE_APPHOST_TOKEN"] = new('b', 64);
        Assert.Equal(new string('b', 64), NativeServerConfiguration.Parse(configuration, launchedByCli: true).RpcToken);

        configuration["ASPIRE_REMOTE_APPHOST_TOKEN"] = null;
        Assert.Throws<InvalidOperationException>(() => NativeServerConfiguration.Parse(configuration, launchedByCli: true));
    }

    [Fact]
    public void ConfigurationCannotDeserializeClockObjectsIntoTheKernel()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(
            """{"runtime":{"timeProvider":{}}}""", NativeServerJsonContext.Default.NativeServerOptions));
    }

    [Fact]
    public void StartupArgumentsAreTestableWithoutOpeningSockets()
    {
        var endpoint = Assert.IsType<IPEndPoint>(NativeStartupArguments.Parse(["--port", "0"], configuredSocket: null));
        Assert.Equal(IPAddress.Loopback, endpoint.Address);
        Assert.Equal(0, endpoint.Port);
        Assert.Throws<ArgumentException>(() => NativeStartupArguments.Parse(["--port", "65536"], configuredSocket: null));
        Assert.Throws<ArgumentException>(() => NativeStartupArguments.Parse(["--socket", "relative"], configuredSocket: null));
    }
}
