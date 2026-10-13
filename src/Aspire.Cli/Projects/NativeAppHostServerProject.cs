// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Nodes;
using Aspire.Cli.Bundles;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Utils;
using Aspire.Hosting;
using Aspire.Shared;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Projects;

// The opt-in server preserves the CLI's bootstrap and backchannel RPC contracts.
internal sealed class NativeAppHostServerProject : IAppHostServerProject, IDisposable
{
    internal const string ExecutableEnvironmentVariable = "ASPIRE_CLI_NATIVE_APPHOST_SERVER";
    private readonly string _executable;
    private readonly IProcessExecutionFactory _processes;
    private readonly ILogger _logger;
    private DirectoryInfo? _directory;
    private string? _generatedSdkDirectory;
    private int _preparationCount;
    private int _runCount;

    public NativeAppHostServerProject(string appPath, string executable, IProcessExecutionFactory processes, ILogger logger)
    {
        if (OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("AppHost Server v2 currently requires Unix-domain sockets.");
        }
        if (!Path.IsPathFullyQualified(executable) || !File.Exists(executable))
        {
            throw new ArgumentException("AppHost Server v2 requires an existing absolute executable path.", nameof(executable));
        }
        AppDirectoryPath = appPath;
        _executable = executable;
        _processes = processes;
        _logger = logger;
    }

    public string AppDirectoryPath { get; }
    internal BundleLayoutLease? LayoutLease { get; init; }
    public string GetInstanceIdentifier() => AppDirectoryPath;

    public async Task<AppHostServerPrepareResult> PrepareAsync(string sdkVersion, IEnumerable<IntegrationReference> integrations,
        string? requestedChannel = null, string? packageSourceOverride = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _generatedSdkDirectory = null;
        var references = integrations.ToArray();
        if (references.Any(reference => reference.Source is not IntegrationSource.Npm &&
            (reference.Source != IntegrationSource.Nuget || reference.Name != "Aspire.Hosting" &&
             reference.Name != "Aspire.Hosting.CodeGeneration.TypeScript")))
        {
            throw new NotSupportedException("AppHost Server v2 requires protocol-v2 provider packages. Managed integration assemblies cannot be loaded into this server.");
        }

        var providers = new JsonArray();
        var packageNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var reference in references.Where(reference => reference.Source == IntegrationSource.Npm))
        {
            var entryPoint = reference.Path ?? throw new InvalidDataException($"Provider '{reference.Name}' has no entry point.");
            var root = Path.GetDirectoryName(entryPoint)!;
            var descriptorPath = Path.Combine(root, "aspire.apphost-server.json");
            if (!File.Exists(descriptorPath))
            {
                throw new NotSupportedException($"Provider '{reference.Name}' does not declare AppHost Server v2 support in aspire.apphost-server.json.");
            }
            var descriptor = JsonNode.Parse(await File.ReadAllTextAsync(descriptorPath, cancellationToken).ConfigureAwait(false)) as JsonObject;
            if (descriptor is null || descriptor["protocolVersion"]?.GetValue<int>() != 2 ||
                descriptor["packageName"] is not JsonValue package ||
                !package.TryGetValue<string>(out var packageName) || string.IsNullOrWhiteSpace(packageName) ||
                !packageNames.Add(packageName) ||
                descriptor["metadata"] is not JsonValue metadataFile ||
                !metadataFile.TryGetValue<string>(out var metadataPath) ||
                descriptor["sdkFiles"] is not JsonObject { Count: <= 128 } declaredFiles ||
                descriptor["exports"] is not JsonArray { Count: <= 128 } exportedModules)
            {
                throw new InvalidDataException($"Provider '{reference.Name}' requires a valid, unique protocol-v2 package descriptor.");
            }
            var metadata = ResolvePackageFile(root, metadataPath);
            var sdkFiles = new JsonObject();
            foreach (var file in declaredFiles)
            {
                sdkFiles[file.Key] = ResolvePackageFile(root, file.Value!.GetValue<string>());
            }
            providers.Add((JsonNode)new JsonObject
            {
                ["packageName"] = packageName,
                ["metadata"] = metadata,
                ["sdkFiles"] = sdkFiles,
                ["exports"] = exportedModules.DeepClone(),
                ["executable"] = "node",
                ["arguments"] = new JsonArray(entryPoint)
            });
        }
        if (providers.Count > 0)
        {
            var managed = LayoutLease?.Layout.GetManagedPath();
            var contract = Path.Combine(Path.GetDirectoryName(_executable)!, "ats-contract.json");
            if (managed is null || !File.Exists(managed) || !File.Exists(contract))
            {
                throw new InvalidOperationException("AppHost Server v2 provider SDK generation requires the matching Aspire bundle tooling and ATS contract.");
            }
            _directory ??= Directory.CreateTempSubdirectory("aspire-apphost-server-");
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(_directory.FullName, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
            var generatedSdkDirectory = Path.Combine(_directory.FullName, $"sdk-{++_preparationCount}");
            var requestPath = Path.Combine(_directory.FullName, "codegen-request.json");
            await File.WriteAllTextAsync(requestPath, new JsonObject
            {
                ["coreContract"] = contract,
                ["output"] = generatedSdkDirectory,
                ["providers"] = providers.DeepClone()
            }.ToJsonString(), cancellationToken).ConfigureAwait(false);
            var output = new OutputCollector();
            var start = new ProcessStartInfo(managed) { WorkingDirectory = AppDirectoryPath, UseShellExecute = false };
            start.ArgumentList.Add("apphost-codegen-v2");
            start.ArgumentList.Add(requestPath);
            LayoutLease!.AddEnvironment(start);
            await using var execution = _processes.CreateExecution(start, new ProcessInvocationOptions
            {
                StandardOutputCallback = output.AppendOutput,
                StandardErrorCallback = output.AppendError
            });
            await execution.StartAsync(cancellationToken).ConfigureAwait(false);
            var exitCode = await execution.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            if (exitCode != 0)
            {
                return new AppHostServerPrepareResult(false, output);
            }
            var runtimeProviders = new JsonArray();
            foreach (var provider in providers)
            {
                runtimeProviders.Add((JsonNode)new JsonObject
                {
                    ["packageName"] = provider!["packageName"]!.DeepClone(),
                    ["executable"] = provider["executable"]!.DeepClone(),
                    ["arguments"] = provider["arguments"]!.DeepClone()
                });
            }
            await File.WriteAllTextAsync(Path.Combine(generatedSdkDirectory, "providers.json"),
                runtimeProviders.ToJsonString(), cancellationToken).ConfigureAwait(false);
            _generatedSdkDirectory = generatedSdkDirectory;
        }

        return new AppHostServerPrepareResult(true, null, NeedsCodeGeneration: true);
    }

    private static string ResolvePackageFile(string root, string relativePath)
    {
        var path = Path.GetFullPath(relativePath, root);
        var relative = Path.GetRelativePath(root, path);
        if (Path.IsPathRooted(relativePath) || Path.IsPathRooted(relative) ||
            relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
            !File.Exists(path))
        {
            throw new InvalidDataException($"Provider asset '{relativePath}' must exist inside its package.");
        }

        return path;
    }

    public async Task<AppHostServerRunResult> RunAsync(int hostPid,
        IReadOnlyDictionary<string, string>? environmentVariables, string[]? additionalArgs, bool debug,
        AppHostServerRunControl? runControl)
    {
        if (additionalArgs is { Length: > 0 })
        {
            throw new NotSupportedException("AppHost Server v2 supports run, not publishing command-line modes.");
        }
        if (debug)
        {
            throw new NotSupportedException("AppHost Server v2 does not support server debugging.");
        }
        var startInfo = new ProcessStartInfo(_executable) { WorkingDirectory = AppDirectoryPath, UseShellExecute = false };
        startInfo.ArgumentList.Add("server");
        startInfo.ArgumentList.Add("--contentRoot");
        startInfo.ArgumentList.Add(AppDirectoryPath);
        if (environmentVariables is not null)
        {
            foreach (var (key, value) in environmentVariables)
            {
                startInfo.Environment[key] = value;
            }
        }
        LayoutLease?.AddEnvironment(startInfo);
        var bootstrap = environmentVariables?.GetValueOrDefault(KnownConfigNames.IntegrationHostBootstrap) == "true";
        if (bootstrap)
        {
            // SDK bootstrap has no graph to execute. Controller and Dashboard
            // lifetimes belong to the final server, not this temporary process.
            startInfo.Environment.Remove("ASPIRE_NATIVE_DCP_PATH");
            startInfo.Environment.Remove("ASPIRE_DASHBOARD_PATH");
            startInfo.Environment.Remove("ASPIRE_NATIVE_DASHBOARD_PORT");
            startInfo.Environment.Remove("AppHost__FilePath");
        }
        if (!bootstrap && LayoutLease?.Layout.GetDcpPath() is { } dcpDirectory)
        {
            startInfo.Environment["ASPIRE_NATIVE_DCP_PATH"] = BundleDiscovery.GetDcpExecutablePath(dcpDirectory);
        }
        if (!bootstrap && LayoutLease?.Layout.GetDashboardPath() is { } dashboardPath)
        {
            startInfo.Environment["ASPIRE_DASHBOARD_PATH"] = dashboardPath;
            startInfo.Environment["ASPIRE_NATIVE_DASHBOARD_PORT"] = "0";
        }
        startInfo.Environment.Remove("ASPIRE_APPHOST_SERVER_V2_SDK_DIRECTORY");
        if (_generatedSdkDirectory is not null)
        {
            startInfo.Environment["ASPIRE_APPHOST_SERVER_V2_SDK_DIRECTORY"] = _generatedSdkDirectory;
        }
        _directory ??= Directory.CreateTempSubdirectory("aspire-native-");
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(_directory.FullName, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        // A bootstrap process can be killed before unlinking its socket. Never
        // reuse that pathname for the final execution session.
        var socketPath = Path.Combine(_directory.FullName, $"apphost-{++_runCount}.sock");
        startInfo.Environment["REMOTE_APP_HOST_SOCKET_PATH"] = socketPath;
        var output = new OutputCollector();
        var execution = _processes.CreateExecution(startInfo, new ProcessInvocationOptions
        {
            StandardOutputCallback = line => { output.AppendOutput(line); _logger.LogDebug("Native AppHost stdout: {Line}", line); },
            StandardErrorCallback = line => { output.AppendError(line); _logger.LogDebug("Native AppHost stderr: {Line}", line); },
            Lifetime = ChildProcessLifetime.AppHost,
            IsolateConsole = runControl?.IsolateConsole ?? false,
            KillOnParentExit = runControl?.KillOnParentExit ?? false,
            GracefulShutdownSignaler = runControl?.GracefulShutdownSignaler,
            ShutdownService = runControl?.ShutdownService,
            KillEntireProcessTreeOnCancel = true
        });
        try
        {
            await execution.StartAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            await execution.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        return new AppHostServerRunResult(socketPath, output, execution);
    }

    public void Dispose()
    {
        try
        {
            _directory?.Delete(recursive: true);
            _directory = null;
        }
        finally
        {
            LayoutLease?.Dispose();
        }
    }
}
