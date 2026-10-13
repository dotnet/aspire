// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Configuration;
using Aspire.Cli.Bundles;
using Aspire.Cli.Layout;
using Aspire.Cli.NuGet;
using Aspire.Cli.Tests.Mcp;
using Aspire.Cli.Projects;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Aspire.Shared;
using Aspire.Hosting;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Cli.Tests.Projects;

public class NativeAppHostServerProjectTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task FactoryUsesTheTargetAppHostConfigAndIgnoresOverrideWithoutOptIn()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var enabled = Directory.CreateDirectory(Path.Combine(workspace.Path, "enabled"));
        var nested = enabled.CreateSubdirectory("nested");
        var disabled = Directory.CreateDirectory(Path.Combine(workspace.Path, "disabled"));
        var executable = Path.Combine(workspace.Path, "server");
        await File.WriteAllTextAsync(executable, "server");
        new AspireConfigFile { Features = new() { [KnownFeatures.AppHostServerV2] = true } }.Save(enabled.FullName);
        new AspireConfigFile { Features = new() { [KnownFeatures.AppHostServerV2] = false } }.Save(disabled.FullName);
        var factory = CreateFactory(new TestEnvironment(new Dictionary<string, string?>
        {
            [NativeAppHostServerProject.ExecutableEnvironmentVariable] = executable
        }), new NullBundleService(), new TestProcessExecutionFactory());

        using var project = Assert.IsType<NativeAppHostServerProject>(await factory.CreateAsync(nested.FullName));
        Assert.Equal(nested.FullName, project.AppDirectoryPath);
        var managed = await factory.CreateAsync(disabled.FullName);
        Assert.IsNotType<NativeAppHostServerProject>(managed);
        (managed as IDisposable)?.Dispose();
    }

    [Fact]
    public async Task FactoryDoesNotFallBackWhenOptedInBundleLacksV2()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        new AspireConfigFile { Features = new() { [KnownFeatures.AppHostServerV2] = true } }.Save(workspace.Path);
        var factory = CreateFactory(new TestEnvironment(), new NullBundleService(), new TestProcessExecutionFactory());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => factory.CreateAsync(workspace.Path));
        Assert.Contains("does not contain AppHost Server v2", exception.Message);
    }

    [Fact]
    public async Task FactoryOverrideRetainsBundleToolingForProviderPreparation()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        new AspireConfigFile { Features = new() { [KnownFeatures.AppHostServerV2] = true } }.Save(workspace.Path);
        var serverDirectory = workspace.CreateDirectory("developer-server");
        var executable = Path.Combine(serverDirectory.FullName, "server");
        var contract = Path.Combine(serverDirectory.FullName, "ats-contract.json");
        await File.WriteAllTextAsync(executable, "");
        await File.WriteAllTextAsync(contract, "{}");
        var managedDirectory = workspace.CreateDirectory("managed");
        var managed = Path.Combine(managedDirectory.FullName, "aspire-managed");
        await File.WriteAllTextAsync(managed, "");
        var providerDirectory = workspace.CreateDirectory("provider");
        var entryPoint = Path.Combine(providerDirectory.FullName, "host.mjs");
        await File.WriteAllTextAsync(entryPoint, "");
        await File.WriteAllTextAsync(Path.Combine(providerDirectory.FullName, "metadata.json"), "{}");
        await File.WriteAllTextAsync(Path.Combine(providerDirectory.FullName, "aspire.apphost-server.json"),
            """{"protocolVersion":2,"packageName":"example.provider","metadata":"metadata.json","sdkFiles":{},"exports":[]}""");
        var layout = new LayoutConfiguration { LayoutPath = workspace.Path };
        var lease = new BundleLayoutLease(layout, null);
        var bundle = new TestBundleService(true) { CreateLayoutLease = () => lease };
        var processes = new TestProcessExecutionFactory();
        var invocations = 0;
        processes.CreateExecutionFromStartInfoCallback = (start, options) =>
        {
            invocations++;
            Assert.Equal(managed, start.FileName);
            Assert.Equal("apphost-codegen-v2", start.ArgumentList[0]);
            var request = JsonNode.Parse(File.ReadAllText(start.ArgumentList[1]))!;
            Assert.Equal(contract, request["coreContract"]!.GetValue<string>());
            Directory.CreateDirectory(request["output"]!.GetValue<string>());
            return new TestProcessExecution(start.FileName, [], null, options,
                (_, _, _) => Task.FromResult((0, (string?)null)), () => 1);
        };
        var factory = CreateFactory(new TestEnvironment(new Dictionary<string, string?>
        {
            [NativeAppHostServerProject.ExecutableEnvironmentVariable] = executable
        }), bundle, processes);
        using var project = Assert.IsType<NativeAppHostServerProject>(await factory.CreateAsync(workspace.Path));

        Assert.Same(lease, project.LayoutLease);
        Assert.Equal(1, bundle.AcquireLayoutCallCount);
        Assert.True((await project.PrepareAsync("17.0", [IntegrationReference.FromNpm("example.provider", entryPoint)])).Success);
        Assert.Equal(1, invocations);
    }

    [Fact]
    public void RejectsMissingExecutable()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        Assert.Throws<ArgumentException>(() => new NativeAppHostServerProject(workspace.Path,
            Path.Combine(workspace.Path, "missing"), new TestProcessExecutionFactory(), NullLogger.Instance));
    }

    [Fact]
    public async Task PreparesSupportedToolingAndRejectsUnportedIntegrations()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var executable = Path.Combine(workspace.Path, "server");
        await File.WriteAllTextAsync(executable, "");
        using var project = new NativeAppHostServerProject(workspace.Path, executable, new TestProcessExecutionFactory(), NullLogger.Instance);
        Assert.True((await project.PrepareAsync("13.6", [
            IntegrationReference.FromPackage("Aspire.Hosting", "13.6"),
            IntegrationReference.FromPackage("Aspire.Hosting.CodeGeneration.TypeScript", "13.6")
        ])).NeedsCodeGeneration);
        await Assert.ThrowsAsync<NotSupportedException>(() => project.PrepareAsync("13.6",
            [IntegrationReference.FromPackage("Aspire.Hosting.Redis", "13.6")]));
        await Assert.ThrowsAsync<NotSupportedException>(() => project.PrepareAsync("13.6",
            [IntegrationReference.FromPackage("Aspire.Hosting.CodeGeneration.Python", "13.6")]));
    }

    [Fact]
    public async Task UsesProcessFactoryAuthenticationPrivateSocketAndRunLifetime()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var executable = Path.Combine(workspace.Path, "server");
        await File.WriteAllTextAsync(executable, "");
        var factory = new TestProcessExecutionFactory();
        string? directory = null;
        factory.CreateExecutionFromStartInfoCallback = (start, options) =>
        {
            Assert.Equal(executable, start.FileName);
            Assert.Equal(workspace.Path, start.WorkingDirectory);
            Assert.Equal(["server", "--contentRoot", workspace.Path], start.ArgumentList);
            Assert.Equal("test-token", start.Environment["ASPIRE_REMOTE_APPHOST_TOKEN"]);
            directory = Path.GetDirectoryName(start.Environment["REMOTE_APP_HOST_SOCKET_PATH"])!;
            if (!OperatingSystem.IsWindows())
            {
                Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute, File.GetUnixFileMode(directory));
            }
            Assert.Equal(ChildProcessLifetime.AppHost, options.Lifetime);
            Assert.True(options.IsolateConsole);
            Assert.True(options.KillEntireProcessTreeOnCancel);
            return new TestProcessExecution(start.FileName, [], null, options,
                (_, _, _) => Task.FromResult((0, (string?)null)), () => 1);
        };
        using (var project = new NativeAppHostServerProject(workspace.Path, executable, factory, NullLogger.Instance))
        {
            var result = await project.RunAsync(42, new Dictionary<string, string> { ["ASPIRE_REMOTE_APPHOST_TOKEN"] = "test-token" },
                [], false, new AppHostServerRunControl(IsolateConsole: true));
            Assert.Equal(Path.Combine(directory!, "apphost-1.sock"), result.SocketPath);
            await result.Execution.DisposeAsync();
            await File.WriteAllTextAsync(result.SocketPath, "stale bootstrap socket");
            var finalSession = await project.RunAsync(42,
                new Dictionary<string, string> { ["ASPIRE_REMOTE_APPHOST_TOKEN"] = "test-token" },
                [], false, new AppHostServerRunControl(IsolateConsole: true));
            Assert.Equal(Path.Combine(directory!, "apphost-2.sock"), finalSession.SocketPath);
            Assert.True(File.Exists(result.SocketPath));
            await finalSession.Execution.DisposeAsync();
        }
        Assert.False(Directory.Exists(directory));
    }

    [Fact]
    public async Task RunReplacesInheritedParentIdentityWithTheLaunchingProcess()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var executable = Path.Combine(workspace.Path, "server");
        await File.WriteAllTextAsync(executable, "");
        var processes = new TestProcessExecutionFactory();
        var pid = Environment.ProcessId;
        var started = ProcessStartTimeHelper.TryGetProcessStartTimeUnixMilliseconds(pid);
        Assert.NotNull(started);
        processes.CreateExecutionFromStartInfoCallback = (start, options) =>
        {
            Assert.Equal(pid.ToString(System.Globalization.CultureInfo.InvariantCulture),
                start.Environment[KnownConfigNames.CliProcessId]);
            Assert.Equal(start.Environment[KnownConfigNames.CliProcessId],
                start.Environment[KnownConfigNames.RemoteAppHostProcessId]);
            Assert.Equal(started.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                start.Environment[KnownConfigNames.CliProcessStartedStable]);
            Assert.Equal(start.Environment[KnownConfigNames.CliProcessStartedStable],
                start.Environment[KnownConfigNames.RemoteAppHostProcessStarted]);
            Assert.Equal(ProcessStartTimeHelper.TryGetRuntimeProcessStartTimeUnixSeconds(pid)!.Value
                .ToString(System.Globalization.CultureInfo.InvariantCulture), start.Environment[KnownConfigNames.CliProcessStarted]);
            return new TestProcessExecution(start.FileName, [], null, options,
                (_, _, _) => Task.FromResult((0, (string?)null)), () => 1);
        };
        using var project = new NativeAppHostServerProject(workspace.Path, executable, processes, NullLogger.Instance);
        var result = await project.RunAsync(pid, new Dictionary<string, string>
        {
            [KnownConfigNames.CliProcessId] = "123",
            [KnownConfigNames.CliProcessStarted] = "1",
            [KnownConfigNames.CliProcessStartedStable] = "1000",
            [KnownConfigNames.RemoteAppHostProcessId] = "456",
            [KnownConfigNames.RemoteAppHostProcessStarted] = "2000"
        }, [], false, null);
        await result.Execution.DisposeAsync();
    }

    [Fact]
    public async Task BootstrapDoesNotLaunchBundledRuntimeComponents()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var executable = Path.Combine(workspace.Path, "server");
        await File.WriteAllTextAsync(executable, "");
        var factory = new TestProcessExecutionFactory();
        factory.CreateExecutionFromStartInfoCallback = (start, options) =>
        {
            Assert.False(start.Environment.ContainsKey("ASPIRE_NATIVE_DCP_PATH"));
            Assert.False(start.Environment.ContainsKey("ASPIRE_DASHBOARD_PATH"));
            Assert.False(start.Environment.ContainsKey("ASPIRE_NATIVE_DASHBOARD_PORT"));
            return new TestProcessExecution(start.FileName, [], null, options,
                (_, _, _) => Task.FromResult((0, (string?)null)), () => 1);
        };
        using var project = new NativeAppHostServerProject(workspace.Path, executable, factory, NullLogger.Instance)
        {
            LayoutLease = new BundleLayoutLease(new LayoutConfiguration { LayoutPath = workspace.Path }, null)
        };

        var result = await project.RunAsync(42, new Dictionary<string, string>
        {
            [KnownConfigNames.IntegrationHostBootstrap] = "true",
            ["ASPIRE_NATIVE_DCP_PATH"] = "inherited-dcp",
            ["ASPIRE_DASHBOARD_PATH"] = "inherited-dashboard",
            ["ASPIRE_NATIVE_DASHBOARD_PORT"] = "0"
        }, [], false, null);
        await result.Execution.DisposeAsync();
    }

    [Theory]
    [InlineData(1, "metadata.json")]
    [InlineData(2, "../outside.json")]
    public async Task RejectsIncompatibleOrEscapingProviderDescriptors(int protocolVersion, string metadata)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var executable = Path.Combine(workspace.Path, "server");
        await File.WriteAllTextAsync(executable, "");
        var root = Directory.CreateDirectory(Path.Combine(workspace.Path, "provider"));
        var entryPoint = Path.Combine(root.FullName, "host.mjs");
        await File.WriteAllTextAsync(entryPoint, "");
        await File.WriteAllTextAsync(Path.Combine(root.FullName, "aspire.apphost-server.json"), new JsonObject
        {
            ["protocolVersion"] = protocolVersion,
            ["packageName"] = "example.provider",
            ["metadata"] = metadata,
            ["sdkFiles"] = new JsonObject(),
            ["exports"] = new JsonArray()
        }.ToJsonString());
        await File.WriteAllTextAsync(Path.Combine(workspace.Path, "outside.json"), "{}");
        using var project = new NativeAppHostServerProject(workspace.Path, executable,
            new TestProcessExecutionFactory(), NullLogger.Instance);

        await Assert.ThrowsAsync<InvalidDataException>(() => project.PrepareAsync("17.0",
            [IntegrationReference.FromNpm("example.provider", entryPoint)]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public async Task ProviderPreparationResetsSdkSelectionAndDoesNotAdvertiseFailedOutput(int exitCode)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var executable = Path.Combine(workspace.Path, "server");
        var managedDirectory = Directory.CreateDirectory(Path.Combine(workspace.Path, "managed"));
        await File.WriteAllTextAsync(executable, "");
        await File.WriteAllTextAsync(Path.Combine(managedDirectory.FullName, "aspire-managed"), "");
        await File.WriteAllTextAsync(Path.Combine(workspace.Path, "ats-contract.json"), "{}");
        var providerDirectory = Directory.CreateDirectory(Path.Combine(workspace.Path, "provider"));
        var entryPoint = Path.Combine(providerDirectory.FullName, "host.mjs");
        await File.WriteAllTextAsync(entryPoint, "");
        await File.WriteAllTextAsync(Path.Combine(providerDirectory.FullName, "metadata.json"), "{}");
        await File.WriteAllTextAsync(Path.Combine(providerDirectory.FullName, "aspire.apphost-server.json"),
            """{"protocolVersion":2,"packageName":"example.provider","metadata":"metadata.json","sdkFiles":{},"exports":[]}""");
        var processes = new TestProcessExecutionFactory();
        string? firstOutput = null;
        processes.CreateExecutionFromStartInfoCallback = (start, options) =>
        {
            if (start.FileName == executable)
            {
                start.Environment.TryGetValue("ASPIRE_APPHOST_SERVER_V2_SDK_DIRECTORY", out var sdk);
                Assert.Equal(exitCode == 0 ? firstOutput : null, sdk);
            }
            else
            {
                var request = JsonNode.Parse(File.ReadAllText(start.ArgumentList[1]))!;
                var output = request["output"]!.GetValue<string>();
                Assert.NotEqual(firstOutput, output);
                firstOutput ??= output;
                Directory.CreateDirectory(output);
            }
            return new TestProcessExecution(start.FileName, [], null, options,
                (_, _, _) => Task.FromResult((start.FileName == executable ? 0 : exitCode, (string?)null)), () => 1);
        };
        using var project = new NativeAppHostServerProject(workspace.Path, executable, processes, NullLogger.Instance)
        {
            LayoutLease = new BundleLayoutLease(new LayoutConfiguration { LayoutPath = workspace.Path }, null)
        };
        var references = new[] { IntegrationReference.FromNpm("example.provider", entryPoint) };

        Assert.Equal(exitCode == 0, (await project.PrepareAsync("17.0", references)).Success);
        var firstRun = await project.RunAsync(42, null, [], false, null);
        await firstRun.Execution.DisposeAsync();
        Assert.Equal(exitCode == 0, (await project.PrepareAsync("17.0", references)).Success);
        Assert.True((await project.PrepareAsync("17.0", [])).Success);
        processes.CreateExecutionFromStartInfoCallback = (start, options) =>
        {
            Assert.False(start.Environment.ContainsKey("ASPIRE_APPHOST_SERVER_V2_SDK_DIRECTORY"));
            return new TestProcessExecution(start.FileName, [], null, options,
                (_, _, _) => Task.FromResult((0, (string?)null)), () => 1);
        };
        var finalRun = await project.RunAsync(42,
            new Dictionary<string, string> { ["ASPIRE_APPHOST_SERVER_V2_SDK_DIRECTORY"] = "stale-sdk" },
            [], false, null);
        await finalRun.Execution.DisposeAsync();
    }

    private static AppHostServerProjectFactory CreateFactory(TestEnvironment environment, IBundleService bundle,
        TestProcessExecutionFactory processes)
        => new(new TestDotNetCliRunner(), MockPackagingServiceFactory.Create(), bundle,
            new BundleNuGetService(NullLogger<BundleNuGetService>.Instance, new FakeNuGetClient()),
            new TestDotNetSdkInstaller(), TestExecutionContextFactory.CreateTestContext(), environment,
            processes, NullLoggerFactory.Instance);
}
