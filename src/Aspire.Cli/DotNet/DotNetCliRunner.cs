// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Configuration;
using Aspire.Cli.Caching;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Aspire.Hosting;
using Aspire.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuGetPackage = Aspire.Shared.NuGetPackageCli;
using System.Security.Cryptography;

namespace Aspire.Cli.DotNet;

internal interface IDotNetCliRunner
{
    Task<(int ExitCode, bool IsAspireHost, string? AspireHostingVersion)> GetAppHostInformationAsync(FileInfo projectFile, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken);
    Task<(int ExitCode, JsonDocument? Output)> GetProjectItemsAndPropertiesAsync(FileInfo projectFile, string[] items, string[] properties, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken);
    Task<int> RunAsync(FileInfo projectFile, bool watch, bool noBuild, string[] args, IDictionary<string, string>? env, TaskCompletionSource<IAppHostBackchannel>? backchannelCompletionSource, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken);
    Task<int> CheckHttpCertificateAsync(DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken);
    Task<int> TrustHttpCertificateAsync(DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken);
    Task<(int ExitCode, string? TemplateVersion)> InstallTemplateAsync(string packageName, string version, FileInfo? nugetConfigFile, string? nugetSource, bool force, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken);
    Task<int> NewProjectAsync(string templateName, string name, string outputPath, string[] extraArgs, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken);
    Task<int> BuildAsync(FileInfo projectFilePath, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken);
    Task<int> AddPackageAsync(FileInfo projectFilePath, string packageName, string packageVersion, string? nugetSource, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken);
    Task<int> AddProjectToSolutionAsync(FileInfo solutionFile, FileInfo projectFile, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken);
    Task<(int ExitCode, NuGetPackage[]? Packages)> SearchPackagesAsync(DirectoryInfo workingDirectory, string query, bool prerelease, int take, int skip, FileInfo? nugetConfigFile, bool useCache, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken);
    Task<(int ExitCode, string[] ConfigPaths)> GetNuGetConfigPathsAsync(DirectoryInfo workingDirectory, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken);
    Task<(int ExitCode, IReadOnlyList<FileInfo> Projects)> GetSolutionProjectsAsync(FileInfo solutionFile, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken);
    Task<int> AddProjectReferenceAsync(FileInfo projectFile, FileInfo referencedProject, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken);
}

internal sealed class DotNetCliRunnerInvocationOptions
{
    public Action<string>? StandardOutputCallback { get; set; }
    public Action<string>? StandardErrorCallback { get; set; }

    public bool NoLaunchProfile { get; set; }
    public bool StartDebugSession { get; set; }
    public bool NoExtensionLaunch { get; set; }
    public bool Debug { get; set; }
}

internal class DotNetCliRunner(ILogger<DotNetCliRunner> logger, IServiceProvider serviceProvider, AspireCliTelemetry telemetry, IConfiguration configuration, IFeatures features, IInteractionService interactionService, CliExecutionContext executionContext, IDiskCache diskCache) : IDotNetCliRunner
{
    private readonly IDiskCache _diskCache = diskCache;

    internal Func<int> GetCurrentProcessId { get; set; } = () => Environment.ProcessId;

    internal Func<long> GetCurrentProcessStartTime { get; set; } = () =>
    {
        var startTime = Process.GetCurrentProcess().StartTime;
        return ((DateTimeOffset)startTime).ToUnixTimeSeconds();
    };

    private string GetMsBuildServerValue()
    {
        return configuration["DOTNET_CLI_USE_MSBUILD_SERVER"] ?? "true";
    }

    // Cache expiry/max age handled inside DiskCache implementation.

    public async Task<(int ExitCode, bool IsAspireHost, string? AspireHostingVersion)> GetAppHostInformationAsync(FileInfo projectFile, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();

        // Get both properties and PackageReference items to determine Aspire.Hosting version
        var (exitCode, jsonDocument) = await GetProjectItemsAndPropertiesAsync(
            projectFile,
            ["PackageReference", "AspireProjectOrPackageReference"],
            ["IsAspireHost", "AspireHostingSDKVersion"],
            options,
            cancellationToken);

        if (exitCode == 0 && jsonDocument != null)
        {
            var rootElement = jsonDocument.RootElement;

            if (!rootElement.TryGetProperty("Properties", out var properties))
            {
                return (exitCode, false, null);
            }

            if (!properties.TryGetProperty("IsAspireHost", out var isAspireHostElement))
            {
                return (exitCode, false, null);
            }

            if (isAspireHostElement.GetString() == "true")
            {
                // Try to get Aspire.Hosting version from PackageReference items
                string? aspireHostingVersion = null;

                if (rootElement.TryGetProperty("Items", out var items))
                {
                    // Check PackageReference items first
                    if (items.TryGetProperty("PackageReference", out var packageReferences))
                    {
                        foreach (var packageRef in packageReferences.EnumerateArray())
                        {
                            if (packageRef.TryGetProperty("Identity", out var identity) &&
                                identity.GetString() == "Aspire.Hosting" &&
                                packageRef.TryGetProperty("Version", out var version))
                            {
                                aspireHostingVersion = version.GetString();
                                break;
                            }
                        }
                    }

                    // Fallback to AspireProjectOrPackageReference items if not found
                    if (aspireHostingVersion == null && items.TryGetProperty("AspireProjectOrPackageReference", out var aspireProjectOrPackageReferences))
                    {
                        foreach (var aspireRef in aspireProjectOrPackageReferences.EnumerateArray())
                        {
                            if (aspireRef.TryGetProperty("Identity", out var identity) &&
                                identity.GetString() == "Aspire.Hosting" &&
                                aspireRef.TryGetProperty("Version", out var version))
                            {
                                aspireHostingVersion = version.GetString();
                                break;
                            }
                        }
                    }
                }

                // If no package version found, fallback to SDK version
                if (aspireHostingVersion == null && properties.TryGetProperty("AspireHostingSDKVersion", out var aspireHostingSdkVersionElement))
                {
                    aspireHostingVersion = aspireHostingSdkVersionElement.GetString();
                }

                return (exitCode, true, aspireHostingVersion);
            }
            else
            {
                return (exitCode, false, null);
            }
        }
        else
        {
            return (exitCode, false, null);
        }
    }

    public async Task<(int ExitCode, JsonDocument? Output)> GetProjectItemsAndPropertiesAsync(FileInfo projectFile, string[] items, string[] properties, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();

        var isSingleFileAppHost = projectFile.Name.Equals("apphost.cs", StringComparison.OrdinalIgnoreCase);
        
        // If we are a single file app host then we use the build command instead of msbuild command.
        var cliArgsList = new List<string> { isSingleFileAppHost ? "build" : "msbuild" };

        if (properties.Length > 0)
        {
            // HACK: MSBuildVersion here because if you ever invoke `dotnet msbuild -getproperty with just a single
            //       property it will not be returned as JSON. I've reported this as a problem to MSBuild but obviously
            //       we need to work around it:
            //
            //       https://github.com/dotnet/msbuild/issues/12490
            //
            cliArgsList.Add($"-getProperty:MSBuildVersion,{string.Join(",", properties)}");
        }

        if (items.Length > 0)
        {
            cliArgsList.Add($"-getItem:{string.Join(",", items)}");
        }

        cliArgsList.Add(projectFile.FullName);

        string[] cliArgs = [.. cliArgsList];

        var stdoutBuilder = new StringBuilder();
        var existingStandardOutputCallback = options.StandardOutputCallback; // Preserve the existing callback if it exists.
        options.StandardOutputCallback = (line) => {
            stdoutBuilder.AppendLine(line);
            existingStandardOutputCallback?.Invoke(line);
        };

        var stderrBuilder = new StringBuilder();
        var existingStandardErrorCallback = options.StandardErrorCallback; // Preserve the existing callback if it exists.
        options.StandardErrorCallback = (line) => {
            stderrBuilder.AppendLine(line);
            existingStandardErrorCallback?.Invoke(line);
        };

        var exitCode = await ExecuteAsync(
            args: cliArgs,
            env: null,
            projectFile: projectFile,
            workingDirectory: projectFile.Directory!,
            backchannelCompletionSource: null,
            options: options,
            cancellationToken: cancellationToken);

        var stdout = stdoutBuilder.ToString();
        var stderr = stderrBuilder.ToString();

        if (exitCode != 0)
        {
            logger.LogError(
                "Failed to get items and properties from project. Exit code was: {ExitCode}. See debug logs for more details. Stderr: {Stderr}, Stdout: {Stdout}",
                exitCode,
                stderr,
                stdout
            );

            return (exitCode, null);
        }
        else
        {
            var json = JsonDocument.Parse(stdout!);
            return (exitCode, json);
        }
    }

    public async Task<int> RunAsync(FileInfo projectFile, bool watch, bool noBuild, string[] args, IDictionary<string, string>? env, TaskCompletionSource<IAppHostBackchannel>? backchannelCompletionSource, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();

        if (watch && noBuild)
        {
            var ex = new InvalidOperationException(ErrorStrings.CantUseBothWatchAndNoBuild);
            backchannelCompletionSource?.SetException(ex);
            throw ex;
        }

        var isSingleFile = projectFile.Extension.Equals(".cs", StringComparison.OrdinalIgnoreCase);
        var watchOrRunCommand = watch ? "watch" : "run";
        var noBuildSwitch = noBuild ? "--no-build" : string.Empty;
        var noProfileSwitch = options.NoLaunchProfile ? "--no-launch-profile" : string.Empty;
        // Add --non-interactive flag when using watch to prevent interactive prompts during automation
        var nonInteractiveSwitch = watch ? "--non-interactive" : string.Empty;
        // Add --verbose flag when using watch and debug is enabled
        var verboseSwitch = watch && options.Debug ? "--verbose" : string.Empty;

        string[] cliArgs = isSingleFile switch
        {
            false => [watchOrRunCommand, nonInteractiveSwitch, verboseSwitch, noBuildSwitch, noProfileSwitch, "--project", projectFile.FullName, "--", .. args],
            true => ["run", noProfileSwitch, "--file", projectFile.FullName, "--", .. args]
        };

        cliArgs = [.. cliArgs.Where(arg => !string.IsNullOrWhiteSpace(arg))];

        // Inject DOTNET_CLI_USE_MSBUILD_SERVER when noBuild == false - we copy the
        // dictionary here because we don't want to mutate the input.
        IDictionary<string, string>? finalEnv = env;
        if (!noBuild)
        {
            finalEnv = new Dictionary<string, string>(env ?? new Dictionary<string, string>())
            {
                ["DOTNET_CLI_USE_MSBUILD_SERVER"] = GetMsBuildServerValue()
            };
        }

        // Check if update notifications are disabled and set version check environment variable
        if (!features.IsFeatureEnabled(KnownFeatures.UpdateNotificationsEnabled, defaultValue: true))
        {
            // Copy the environment if we haven't already
            if (finalEnv == env)
            {
                finalEnv = new Dictionary<string, string>(env ?? new Dictionary<string, string>());
            }

            // Only set the environment variable if it's not already set by the user
            if (finalEnv is not null && !finalEnv.ContainsKey(KnownConfigNames.VersionCheckDisabled))
            {
                finalEnv[KnownConfigNames.VersionCheckDisabled] = "true";
            }
        }

        // Set DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER when watch is enabled to prevent launching browser
        if (watch)
        {
            // Copy the environment if we haven't already
            if (finalEnv == env)
            {
                finalEnv = new Dictionary<string, string>(env ?? new Dictionary<string, string>());
            }

            // Only set the environment variable if it's not already set by the user
            if (finalEnv is not null && !finalEnv.ContainsKey("DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER"))
            {
                finalEnv["DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER"] = "true";
            }
        }

        if (features.IsFeatureEnabled(KnownFeatures.DotNetSdkInstallationEnabled, true))
        {
            if (finalEnv == env)
            {
                finalEnv = new Dictionary<string, string>(env ?? new Dictionary<string, string>());
            }

            // Only set the environment variable if it's not already set by the user
            if (finalEnv is not null && !finalEnv.ContainsKey("DOTNET_ROLL_FORWARD"))
            {
                finalEnv["DOTNET_ROLL_FORWARD"] = "LatestMajor";
            }            
        }

        return await ExecuteAsync(
            args: cliArgs,
            env: finalEnv,
            projectFile: projectFile,
            workingDirectory: projectFile.Directory!,
            backchannelCompletionSource: backchannelCompletionSource,
            options: options,
            cancellationToken: cancellationToken);
    }

    public async Task<int> CheckHttpCertificateAsync(DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();

        string[] cliArgs = ["dev-certs", "https", "--check", "--trust"];
        return await ExecuteAsync(
            args: cliArgs,
            env: null,
            projectFile: null,
            workingDirectory: new DirectoryInfo(Environment.CurrentDirectory),
            backchannelCompletionSource: null,
            options: options,
            cancellationToken: cancellationToken);
    }

    public async Task<int> TrustHttpCertificateAsync(DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();

        string[] cliArgs = ["dev-certs", "https", "--trust"];
        return await ExecuteAsync(
            args: cliArgs,
            env: null,
            projectFile: null,
            workingDirectory: new DirectoryInfo(Environment.CurrentDirectory),
            backchannelCompletionSource: null,
            options: options,
            cancellationToken: cancellationToken);
    }

    public async Task<(int ExitCode, string? TemplateVersion)> InstallTemplateAsync(string packageName, string version, FileInfo? nugetConfigFile, string? nugetSource, bool force, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity(nameof(InstallTemplateAsync), ActivityKind.Client);

        // NOTE: The change to @ over :: for template version separator (now enforced in .NET 10.0 SDK).
        List<string> cliArgs = ["new", "install", $"{packageName}@{version}"];

        if (force)
        {
            cliArgs.Add("--force");
        }

        if (nugetSource is not null)
        {
            cliArgs.Add("--nuget-source");
            cliArgs.Add(nugetSource);
        }

        var stdoutBuilder = new StringBuilder();
        var existingStandardOutputCallback = options.StandardOutputCallback; // Preserve the existing callback if it exists.
        options.StandardOutputCallback = (line) => {
            stdoutBuilder.AppendLine(line);
            existingStandardOutputCallback?.Invoke(line);
        };

        var stderrBuilder = new StringBuilder();
        var existingStandardErrorCallback = options.StandardErrorCallback; // Preserve the existing callback if it exists.
        options.StandardErrorCallback = (line) => {
            stderrBuilder.AppendLine(line);
            existingStandardErrorCallback?.Invoke(line);
        };

        // The dotnet new install command does not support the --configfile option so if we
        // are installing packages based on a channel config we'll be passing in a nuget config
        // file which is dynamically generated in a temporary folder. We'll use that temporary
        // folder as the working directory for the command. If we are using an implicit channel
        // then we just use the current execution context for the CLI and inherit whatever
        // NuGet.configs that may or may not be laying around.
        var workingDirectory = nugetConfigFile?.Directory ?? executionContext.WorkingDirectory;

        var exitCode = await ExecuteAsync(
            args: [.. cliArgs],
            env: new Dictionary<string, string>
            {
                // Force English output for consistent parsing.
                // See NOTE: below
                [KnownConfigNames.DotnetCliUiLanguage] = "en-US"
            },
            projectFile: null,
            workingDirectory: workingDirectory,
            backchannelCompletionSource: null,
            options: options,
            cancellationToken: cancellationToken);

        var stdout = stdoutBuilder.ToString();
        var stderr = stderrBuilder.ToString();

        if (exitCode != 0)
        {
            logger.LogError(
                "Failed to install template {PackageName} with version {Version}. See debug logs for more details. Stderr: {Stderr}, Stdout: {Stdout}",
                packageName,
                version,
                stderr,
                stdout
            );
            return (exitCode, null);
        }
        else
        {
            if (stdout is null)
            {
                logger.LogError("Failed to read stdout from the process. This should never happen.");
                return (ExitCodeConstants.FailedToInstallTemplates, null);
            }

            // NOTE: This parsing logic is hopefully temporary and in the future we'll
            //       have structured output:
            //
            //       See: https://github.com/dotnet/sdk/issues/46345
            //
            if (!TryParsePackageVersionFromStdout(stdout, out var parsedVersion))
            {
                logger.LogError("Failed to parse template version from stdout.");

                // Throwing here because this should never happen - we don't want to return
                // the zero exit code if we can't parse the version because its possibly a
                // signal that the .NET SDK has changed.
                throw new InvalidOperationException(ErrorStrings.FailedToParseTemplateVersionFromStdout);
            }

            return (exitCode, parsedVersion);
        }
    }

    private static bool TryParsePackageVersionFromStdout(string stdout, [NotNullWhen(true)] out string? version)
    {
        var lines = stdout.Split(Environment.NewLine);
        var successLine = lines.SingleOrDefault(x => x.StartsWith("Success: Aspire.ProjectTemplates"));

        if (successLine is null)
        {
            version = null;
            return false;
        }

        var templateVersion = successLine.Split(" ") switch { // Break up the success line.
            { Length: > 2 } chunks => chunks[1].Split("::") switch { // Break up the template+version string
                { Length: 2 } versionChunks => versionChunks[1], // The version in the second chunk
                _ => null
            },
            _ => null
        };

        if (templateVersion is not null)
        {
            version = templateVersion;
            return true;
        }
        else
        {
            version = null;
            return false;
        }
    }

    public async Task<int> NewProjectAsync(string templateName, string name, string outputPath, string[] extraArgs, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();

        string[] cliArgs = ["new", templateName, "--name", name, "--output", outputPath, ..extraArgs];
        return await ExecuteAsync(
            args: cliArgs,
            env: null,
            projectFile: null,
            workingDirectory: new DirectoryInfo(Environment.CurrentDirectory),
            backchannelCompletionSource: null,
            options: options,
            cancellationToken: cancellationToken);
    }

    internal static string GetBackchannelSocketPath()
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var aspireCliPath = Path.Combine(homeDirectory, ".aspire", "cli", "backchannels");

        if (!Directory.Exists(aspireCliPath))
        {
            Directory.CreateDirectory(aspireCliPath);
        }

        var uniqueSocketPathSegment = Guid.NewGuid().ToString("N");
        var socketPath = Path.Combine(aspireCliPath, $"cli.sock.{uniqueSocketPathSegment}");
        return socketPath;
    }

    public virtual async Task<int> ExecuteAsync(string[] args, IDictionary<string, string>? env, FileInfo? projectFile, DirectoryInfo workingDirectory, TaskCompletionSource<IAppHostBackchannel>? backchannelCompletionSource, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();

        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory.FullName,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        if (env is not null)
        {
            foreach (var envKvp in env)
            {
                startInfo.EnvironmentVariables[envKvp.Key] = envKvp.Value;
            }
        }

        foreach (var a in args)
        {
            startInfo.ArgumentList.Add(a);
        }

        var socketPath = GetBackchannelSocketPath();
        if (backchannelCompletionSource is not null)
        {
            startInfo.EnvironmentVariables[KnownConfigNames.UnixSocketPath] = socketPath;
        }

        // The AppHost uses this environment variable to signal to the CliOrphanDetector which process
        // it should monitor in order to know when to stop the CLI. As long as the process still exists
        // the orphan detector will allow the CLI to keep running. If the environment variable does
        // not exist the orphan detector will exit.
        startInfo.EnvironmentVariables[KnownConfigNames.CliProcessId] = GetCurrentProcessId().ToString(CultureInfo.InvariantCulture);

        // Set the CLI process start time for robust orphan detection to prevent PID reuse issues.
        // The AppHost will verify both PID and start time to ensure it's monitoring the correct process.
        if (features.IsFeatureEnabled(KnownFeatures.OrphanDetectionWithTimestampEnabled, true))
        {
            startInfo.EnvironmentVariables[KnownConfigNames.CliProcessStarted] = GetCurrentProcessStartTime().ToString(CultureInfo.InvariantCulture);
        }

        // Always set MSBUILDTERMINALLOGGER=false for all dotnet command executions to ensure consistent terminal logger behavior
        startInfo.EnvironmentVariables[KnownConfigNames.MsBuildTerminalLogger] = "false";

        // Suppress the .NET welcome message that appears on first run
        startInfo.EnvironmentVariables["DOTNET_NOLOGO"] = "1";

        // Configure DOTNET_ROOT to point to the private SDK installation if it exists
        ConfigurePrivateSdkEnvironment(startInfo);

        if (ExtensionHelper.IsExtensionHost(interactionService, out var extensionInteractionService, out var backchannel))
        {
            // Even if AppHost is launched through the CLI, we still need to set the extension capabilities so that supported resource types may be started through VS Code.
            startInfo.EnvironmentVariables[KnownConfigNames.DebugSessionInfo] = configuration[KnownConfigNames.DebugSessionInfo];

            if (backchannelCompletionSource is not null
                && projectFile is not null
                && !options.NoExtensionLaunch
                && await backchannel.HasCapabilityAsync(KnownCapabilities.Project, cancellationToken))
            {
                await extensionInteractionService.LaunchAppHostAsync(
                    projectFile.FullName,
                    startInfo.ArgumentList.ToList(),
                    startInfo.Environment.Select(kvp => new EnvVar { Name = kvp.Key, Value = kvp.Value }).ToList(),
                    options.StartDebugSession);

                _ = StartBackchannelAsync(null, socketPath, backchannelCompletionSource, cancellationToken);

                return ExitCodeConstants.Success;
            }
        }

        var process = new Process { StartInfo = startInfo };

        logger.LogDebug("Running dotnet with args: {Args}", string.Join(" ", args));

        var started = process.Start();

        if (backchannelCompletionSource is not null)
        {
            _ = StartBackchannelAsync(process, socketPath, backchannelCompletionSource, cancellationToken);
        }

        var pendingStdoutStreamForwarder = Task.Run(async () => {
            await ForwardStreamToLoggerAsync(
                process.StandardOutput,
                "stdout",
                process,
                options.StandardOutputCallback,
                cancellationToken);
            }, cancellationToken);

        var pendingStderrStreamForwarder = Task.Run(async () => {
            await ForwardStreamToLoggerAsync(
                process.StandardError,
                "stderr",
                process,
                options.StandardErrorCallback,
                cancellationToken);
            }, cancellationToken);

        if (!started)
        {
            logger.LogDebug("Failed to start dotnet process with args: {Args}", string.Join(" ", args));
            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }
        else
        {
            logger.LogDebug("Started dotnet with PID: {ProcessId}", process.Id);
        }

        logger.LogDebug("Waiting for dotnet process to exit with PID: {ProcessId}", process.Id);

        await process.WaitForExitAsync(cancellationToken);

        if (!process.HasExited)
        {
            logger.LogDebug("dotnet process with PID: {ProcessId} has not exited, killing it.", process.Id);
            process.Kill(false);
        }
        else
        {
            logger.LogDebug("dotnet process with PID: {ProcessId} has exited with code: {ExitCode}", process.Id, process.ExitCode);
        }

        // Wait for all the stream forwarders to finish so we know we've got everything
        // fired off through the callbacks.
        await Task.WhenAll([pendingStdoutStreamForwarder, pendingStderrStreamForwarder]);
        return process.ExitCode;

        async Task ForwardStreamToLoggerAsync(StreamReader reader, string identifier, Process process, Action<string>? lineCallback, CancellationToken cancellationToken)
        {
            logger.LogDebug(
                "Starting to forward stream with identifier '{Identifier}' on process '{ProcessId}' to logger",
                identifier,
                process.Id
                );

            while (!cancellationToken.IsCancellationRequested && !reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                logger.LogDebug(
                    "dotnet({ProcessId}) {Identifier}: {Line}",
                    process.Id,
                    identifier,
                    line
                    );
                lineCallback?.Invoke(line!);
            }
        }
    }

    private async Task StartBackchannelAsync(Process? process, string socketPath, TaskCompletionSource<IAppHostBackchannel> backchannelCompletionSource, CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));

        var backchannel = serviceProvider.GetRequiredService<IAppHostBackchannel>();
        var connectionAttempts = 0;

        logger.LogDebug("Starting backchannel connection to AppHost at {SocketPath}", socketPath);

        var startTime = DateTimeOffset.UtcNow;

        do
        {
            try
            {
                logger.LogTrace("Attempting to connect to AppHost backchannel at {SocketPath} (attempt {Attempt})", socketPath, connectionAttempts++);
                await backchannel.ConnectAsync(socketPath, cancellationToken).ConfigureAwait(false);
                backchannelCompletionSource.SetResult(backchannel);
                backchannel.AddDisconnectHandler((_, _) =>
                {
                    // If the backchannel disconnects, we want to stop the CLI process
                    Environment.Exit(ExitCodeConstants.Success);
                });

                logger.LogDebug("Connected to AppHost backchannel at {SocketPath}", socketPath);
                return;
            }
            catch (SocketException ex) when (process is not null && process.HasExited && process.ExitCode != 0)
            {
                logger.LogError(ex, "AppHost process has exited. Unable to connect to backchannel at {SocketPath}", socketPath);
                var backchannelException = new FailedToConnectBackchannelConnection($"AppHost process has exited unexpectedly. Use --debug to see more details.", process, ex);
                backchannelCompletionSource.SetException(backchannelException);
                return;
            }
            catch (SocketException ex)
            {
                // If the process is taking a long time to open a back channel but
                // it has not exited then it probably means that its a larger build
                // (remember it has to build the apphost and its dependencies).
                // In that case, after 30 seconds we just slow down the polling to
                // once per second.
                var waitingFor = DateTimeOffset.UtcNow - startTime;
                if (waitingFor > TimeSpan.FromSeconds(10))
                {
                    logger.LogTrace(ex, "Slow polling for backchannel connection (attempt {Attempt})", connectionAttempts);
                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // We don't want to spam the logs with our early connection attempts.
                }
            }
            catch (AppHostIncompatibleException ex)
            {
                logger.LogError(
                    ex,
                    "The app host is incompatible with the CLI and must be updated to a version that supports the {RequiredCapability} capability.",
                    ex.RequiredCapability
                    );

                // If the app host is incompatible then there is no point
                // trying to reconnect, we should propogate the exception
                // up to the code that needs to back channel so it can display
                // and error message to the user.
                backchannelCompletionSource.SetException(ex);

                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unexpected error occurred while trying to connect to the backchannel.");
                backchannelCompletionSource.SetException(ex);
                throw;
            }

        } while (await timer.WaitForNextTickAsync(cancellationToken));
    }

    public async Task<int> BuildAsync(FileInfo projectFilePath, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();

        string[] cliArgs = ["build", projectFilePath.FullName];

        // Always inject DOTNET_CLI_USE_MSBUILD_SERVER for apphost builds
        var env = new Dictionary<string, string>
        {
            ["DOTNET_CLI_USE_MSBUILD_SERVER"] = GetMsBuildServerValue()
        };

        return await ExecuteAsync(
            args: cliArgs,
            env: env,
            projectFile: projectFilePath,
            workingDirectory: projectFilePath.Directory!,
            backchannelCompletionSource: null,
            options: options,
            cancellationToken: cancellationToken);
    }
    public async Task<int> AddPackageAsync(FileInfo projectFilePath, string packageName, string packageVersion, string? nugetSource, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();

        var cliArgsList = new List<string>
        {
            "add"
        };

        // For single-file AppHost (apphost.cs), use --file switch instead of positional argument
        var isSingleFileAppHost = projectFilePath.Name.Equals("apphost.cs", StringComparison.OrdinalIgnoreCase);
        if (isSingleFileAppHost)
        {
            cliArgsList.AddRange(["package", "--file", projectFilePath.FullName]);
            // For single-file AppHost, use packageName@version format
            cliArgsList.Add($"{packageName}@{packageVersion}");
        }
        else
        {
            cliArgsList.AddRange([projectFilePath.FullName, "package"]);
            // For non single-file scenarios, use separate --version flag
            cliArgsList.Add(packageName);
            cliArgsList.Add("--version");
            cliArgsList.Add(packageVersion);
        }

        if (string.IsNullOrEmpty(nugetSource))
        {
            cliArgsList.Add("--no-restore");
        }
        else
        {
            cliArgsList.Add("--source");
            cliArgsList.Add(nugetSource);
        }

        string[] cliArgs = [.. cliArgsList];

        logger.LogInformation("Adding package {PackageName} with version {PackageVersion} to project {ProjectFilePath}", packageName, packageVersion, projectFilePath.FullName);

        var result = await ExecuteAsync(
            args: cliArgs,
            env: null,
            projectFile: projectFilePath,
            workingDirectory: projectFilePath.Directory!,
            backchannelCompletionSource: null,
            options: options,
            cancellationToken: cancellationToken);

        if (result != 0)
        {
            logger.LogError("Failed to add package {PackageName} with version {PackageVersion} to project {ProjectFilePath}. See debug logs for more details.", packageName, packageVersion, projectFilePath.FullName);
        }
        else
        {
            logger.LogInformation("Package {PackageName} with version {PackageVersion} added to project {ProjectFilePath}", packageName, packageVersion, projectFilePath.FullName);
        }

        return result;
    }

    public async Task<int> AddProjectToSolutionAsync(FileInfo solutionFile, FileInfo projectFile, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();

        string[] cliArgs = ["sln", solutionFile.FullName, "add", projectFile.FullName];

        logger.LogInformation("Adding project {ProjectFilePath} to solution {SolutionFilePath}", projectFile.FullName, solutionFile.FullName);

        var result = await ExecuteAsync(
            args: cliArgs,
            env: null,
            projectFile: null,
            workingDirectory: solutionFile.Directory!,
            backchannelCompletionSource: null,
            options: options,
            cancellationToken: cancellationToken);

        if (result != 0)
        {
            logger.LogError("Failed to add project {ProjectFilePath} to solution {SolutionFilePath}. See debug logs for more details.", projectFile.FullName, solutionFile.FullName);
        }
        else
        {
            logger.LogInformation("Project {ProjectFilePath} added to solution {SolutionFilePath}", projectFile.FullName, solutionFile.FullName);
        }

        return result;
    }

    public async Task<string> ComputeNuGetConfigHierarchySha256Async(DirectoryInfo workingDirectory, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        // The purpose of this method is to compute a hash that can be used as a substitute for an explicitly passed
        // in NuGet.config file hash. This is useful for when `aspire add` is invoked and we present options from the
        // implicit feed where we effectively are presenting cached options based on the NuGet.config config in the
        // current working directory. If any NuGet.config in the hierarchy of NuGet.config files is touched then the
        // cache will be invalidated and we'll do a live search instead of using the cache. This is necessary for
        // implicit channel searches which generally provide the best choice to users in the case of `aspire add`.

        ArgumentNullException.ThrowIfNull(workingDirectory);

        using var activity = telemetry.ActivitySource.StartActivity(nameof(ComputeNuGetConfigHierarchySha256Async));

        var (exitCode, configPaths) = await GetNuGetConfigPathsAsync(workingDirectory, options, cancellationToken);

        if (exitCode != 0)
        {
            logger.LogError("Failed to get NuGet config paths. Exit code was: {ExitCode}.", exitCode);
            return string.Empty;
        }

        if (configPaths.Length == 0)
        {
            return string.Empty;
        }

        var hashes = new List<string>();

        foreach (var configPath in configPaths)
        {
            if (string.IsNullOrWhiteSpace(configPath))
            {
                continue;
            }

            var filePath = Path.IsPathRooted(configPath)
                ? configPath
                : Path.Combine(workingDirectory.FullName, configPath);

            if (!File.Exists(filePath))
            {
                logger.LogDebug("NuGet config file not found at path: {Path}", filePath);
                continue;
            }

            try
            {
                using var stream = File.OpenRead(filePath);
                var bytes = await SHA256.HashDataAsync(stream, cancellationToken);
                var hex = Convert.ToHexString(bytes);
                hashes.Add(hex);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                logger.LogDebug(ex, "Failed to read or hash NuGet config file at path: {Path}", filePath);
                continue;
            }
        }

        var result = string.Join("|", hashes);
        return result;
    }

    public async Task<(int ExitCode, NuGetPackage[]? Packages)> SearchPackagesAsync(DirectoryInfo workingDirectory, string query, bool prerelease, int take, int skip, FileInfo? nugetConfigFile, bool useCache, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();

        string? rawKey = null;
        bool cacheEnabled = useCache && features.IsFeatureEnabled(KnownFeatures.PackageSearchDiskCachingEnabled, defaultValue: true);
        if (cacheEnabled)
        {
            try
            {
                // Compute optional hash of the nuget.config file contents (if any)
                string nugetConfigHash = string.Empty;
                if (nugetConfigFile is not null && nugetConfigFile.Exists)
                {
                    using var stream = nugetConfigFile.OpenRead();
                    var bytes = await SHA256.HashDataAsync(stream, cancellationToken);
                    nugetConfigHash = Convert.ToHexString(bytes);
                }
                else
                {
                    nugetConfigHash = await ComputeNuGetConfigHierarchySha256Async(workingDirectory, options, cancellationToken);
                }

                // Build a cache key using the main discriminators, including CLI version.
                var cliVersion = VersionHelper.GetDefaultTemplateVersion();
                rawKey = $"query={query}|prerelease={prerelease}|take={take}|skip={skip}|nugetConfigHash={nugetConfigHash}|cliVersion={cliVersion}";
                var cached = await _diskCache.GetAsync(rawKey, cancellationToken).ConfigureAwait(false);
                if (cached is not null)
                {
                    try
                    {
                        var foundPackages = PackageUpdateHelpers.ParsePackageSearchResults(cached);
                        return (0, foundPackages.ToArray());
                    }
                    catch (JsonException ex)
                    {
                        logger.LogDebug(ex, "Failed to parse cached package search JSON; performing live search.");
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                // Fail open – treat as cache miss.
                logger.LogDebug(ex, "Failed to probe package search disk cache; proceeding without cache.");
                cacheEnabled = false; // disable write attempt as well
            }
        }

        List<string> cliArgs = [
            "package",
            "search",
            query,
            "--take",
            take.ToString(CultureInfo.InvariantCulture),
            "--skip",
            skip.ToString(CultureInfo.InvariantCulture),
            "--format",
            "json"
        ];

        if (nugetConfigFile is not null)
        {
            cliArgs.Add("--configfile");
            cliArgs.Add(nugetConfigFile.FullName);
        }

        if (prerelease)
        {
            cliArgs.Add("--prerelease");
        }

        var stdoutBuilder = new StringBuilder();
        var existingStandardOutputCallback = options.StandardOutputCallback; // Preserve the existing callback if it exists.
        options.StandardOutputCallback = (line) =>
        {
            stdoutBuilder.AppendLine(line);
            existingStandardOutputCallback?.Invoke(line);
        };

        var stderrBuilder = new StringBuilder();
        var existingStandardErrorCallback = options.StandardErrorCallback; // Preserve the existing callback if it exists.
        options.StandardErrorCallback = (line) =>
        {
            stderrBuilder.AppendLine(line);
            existingStandardErrorCallback?.Invoke(line);
        };

        var result = await ExecuteAsync(
            args: cliArgs.ToArray(),
            env: null,
            projectFile: null,
            workingDirectory: workingDirectory!,
            backchannelCompletionSource: null,
            options: options,
            cancellationToken: cancellationToken);

        var stdout = stdoutBuilder.ToString();
        var stderr = stderrBuilder.ToString();

        if (result != 0)
        {
            logger.LogError(
                "Failed to search for packages. See debug logs for more details. Stderr: {Stderr}, Stdout: {Stdout}",
                stderr,
                stdout
                );
            return (result, null);
        }
        else
        {
            if (stdout is null)
            {
                logger.LogError("Failed to read stdout from the process. This should never happen.");
                return (ExitCodeConstants.FailedToAddPackage, null);
            }

            try
            {
                var foundPackages = PackageUpdateHelpers.ParsePackageSearchResults(stdout);

                // Attempt to persist the raw stdout JSON for future lookups when cache enabled
                if (cacheEnabled && rawKey is not null)
                {
                    await _diskCache.SetAsync(rawKey, stdout, cancellationToken).ConfigureAwait(false);
                }

                return (result, foundPackages.ToArray());
            }
            catch (JsonException ex)
            {
                logger.LogError($"Failed to read JSON returned by the package search. {ex.Message}");
                return (ExitCodeConstants.FailedToAddPackage, null);
            }

        }
    }

    public async Task<(int ExitCode, string[] ConfigPaths)> GetNuGetConfigPathsAsync(DirectoryInfo workingDirectory, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();

        string[] cliArgs = ["nuget", "config", "paths"];

        var stdoutLines = new List<string>();
        var existingStandardOutputCallback = options.StandardOutputCallback; // Preserve the existing callback if it exists.
        options.StandardOutputCallback = (line) => {
            stdoutLines.Add(line);
            existingStandardOutputCallback?.Invoke(line);
        };

        var stderrLines = new List<string>();
        var existingStandardErrorCallback = options.StandardErrorCallback; // Preserve the existing callback if it exists.
        options.StandardErrorCallback = (line) => {
            stderrLines.Add(line);
            existingStandardErrorCallback?.Invoke(line);
        };

        var exitCode = await ExecuteAsync(
            args: cliArgs,
            env: null,
            projectFile: null,
            workingDirectory: workingDirectory,
            backchannelCompletionSource: null,
            options: options,
            cancellationToken: cancellationToken);

        if (exitCode != 0)
        {
            logger.LogError("Failed to get NuGet config paths. Exit code was: {ExitCode}.", exitCode);
            return (exitCode, Array.Empty<string>());
        }
        else
        {
            return (exitCode, stdoutLines.ToArray());
        }
    }

    public async Task<(int ExitCode, IReadOnlyList<FileInfo> Projects)> GetSolutionProjectsAsync(FileInfo solutionFile, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();

        string[] cliArgs = ["sln", solutionFile.FullName, "list"];

        var stdoutLines = new List<string>();
        var existingStandardOutputCallback = options.StandardOutputCallback;
        options.StandardOutputCallback = (line) => {
            stdoutLines.Add(line);
            existingStandardOutputCallback?.Invoke(line);
        };

        var stderrLines = new List<string>();
        var existingStandardErrorCallback = options.StandardErrorCallback;
        options.StandardErrorCallback = (line) => {
            stderrLines.Add(line);
            existingStandardErrorCallback?.Invoke(line);
        };

        var exitCode = await ExecuteAsync(
            args: cliArgs,
            env: null,
            projectFile: null,
            workingDirectory: solutionFile.Directory!,
            backchannelCompletionSource: null,
            options: options,
            cancellationToken: cancellationToken);

        if (exitCode != 0)
        {
            logger.LogError("Failed to list solution projects. Exit code was: {ExitCode}.", exitCode);
            return (exitCode, Array.Empty<FileInfo>());
        }

        // Parse output - skip header lines (Project(s) and ----------)
        var projects = new List<FileInfo>();
        var startParsing = false;
        
        foreach (var line in stdoutLines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            // Skip header lines
            if (line.StartsWith("Project(s)", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("----------", StringComparison.Ordinal))
            {
                startParsing = true;
                continue;
            }

            if (startParsing && line.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                var projectPath = Path.IsPathRooted(line)
                    ? line
                    : Path.Combine(solutionFile.Directory!.FullName, line);
                projects.Add(new FileInfo(projectPath));
            }
        }

        return (exitCode, projects);
    }

    public async Task<int> AddProjectReferenceAsync(FileInfo projectFile, FileInfo referencedProject, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();

        string[] cliArgs = ["add", projectFile.FullName, "reference", referencedProject.FullName];

        logger.LogInformation("Adding project reference from {ProjectFile} to {ReferencedProject}", projectFile.FullName, referencedProject.FullName);

        var result = await ExecuteAsync(
            args: cliArgs,
            env: null,
            projectFile: projectFile,
            workingDirectory: projectFile.Directory!,
            backchannelCompletionSource: null,
            options: options,
            cancellationToken: cancellationToken);

        if (result != 0)
        {
            logger.LogError("Failed to add project reference from {ProjectFile} to {ReferencedProject}. See debug logs for more details.", projectFile.FullName, referencedProject.FullName);
        }
        else
        {
            logger.LogInformation("Project reference added from {ProjectFile} to {ReferencedProject}", projectFile.FullName, referencedProject.FullName);
        }

        return result;
    }

    /// <summary>
    /// Configures environment variables to use the private SDK installation if it exists.
    /// </summary>
    /// <param name="startInfo">The process start info to configure.</param>
    private void ConfigurePrivateSdkEnvironment(ProcessStartInfo startInfo)
    {
        // Get the effective minimum SDK version to determine which private SDK to use
        var sdkInstaller = serviceProvider.GetRequiredService<IDotNetSdkInstaller>();
        var sdkVersion = sdkInstaller.GetEffectiveMinimumSdkVersion();
        var sdksDirectory = executionContext.SdksDirectory.FullName;
        var sdkInstallPath = Path.Combine(sdksDirectory, "dotnet", sdkVersion);
        var dotnetExecutablePath = Path.Combine(
            sdkInstallPath,
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet"
        );

        // Check if the private SDK exists
        if (Directory.Exists(sdkInstallPath))
        {
            // Set the executable path to be the private SDK.
            startInfo.FileName = dotnetExecutablePath;

            // Set DOTNET_ROOT to point to the private SDK installation
            startInfo.EnvironmentVariables["DOTNET_ROOT"] = sdkInstallPath;
            
            // Also set DOTNET_MULTILEVEL_LOOKUP to 0 to prevent fallback to system SDKs
            startInfo.EnvironmentVariables["DOTNET_MULTILEVEL_LOOKUP"] = "0";
            
            // Prepend the private SDK path to PATH so the dotnet executable from the private installation is found first
            var currentPath = startInfo.EnvironmentVariables["PATH"] ?? Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            startInfo.EnvironmentVariables["PATH"] = $"{sdkInstallPath}{Path.PathSeparator}{currentPath}";
            
            logger.LogDebug("Using private SDK installation at {SdkPath}", sdkInstallPath);
        }
    }
}