// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Aspire.Cli.Backchannel;
using Aspire.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli;

internal interface IDotNetCliRunner
{
    Task<(int ExitCode, bool IsAspireHost, string? AspireHostingSdkVersion)> GetAppHostInformationAsync(FileInfo projectFile, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken);
    Task<(int ExitCode, JsonDocument? Output)> GetProjectItemsAndPropertiesAsync(FileInfo projectFile, string[] items, string[] properties, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken);
    Task<int> RunAsync(FileInfo projectFile, bool watch, bool noBuild, string[] args, IDictionary<string, string>? env, TaskCompletionSource<IAppHostBackchannel>? backchannelCompletionSource, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken);
    Task<int> CheckHttpCertificateAsync(DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken);
    Task<int> TrustHttpCertificateAsync(DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken);
    Task<(int ExitCode, string? TemplateVersion)> InstallTemplateAsync(string packageName, string version, string? nugetSource, bool force, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken);
    Task<int> NewProjectAsync(string templateName, string name, string outputPath, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken);
    Task<int> BuildAsync(FileInfo projectFilePath, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken);
    Task<int> AddPackageAsync(FileInfo projectFilePath, string packageName, string packageVersion, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken);
    Task<(int ExitCode, NuGetPackage[]? Packages)> SearchPackagesAsync(DirectoryInfo workingDirectory, string query, bool prerelease, int take, int skip, string? nugetSource, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken);
}

internal sealed class DotNetCliRunnerInvocationOptions
{
    public Action<string>? StandardOutputCallback { get; set; }
    public Action<string>? StandardErrorCallback { get; set; }

    public bool NoLaunchProfile { get; set; }
}

internal class DotNetCliRunner(ILogger<DotNetCliRunner> logger, IServiceProvider serviceProvider) : IDotNetCliRunner
{
    private readonly ActivitySource _activitySource = new ActivitySource(nameof(DotNetCliRunner));

    internal Func<int> GetCurrentProcessId { get; set; } = () => Environment.ProcessId;

    public async Task<(int ExitCode, bool IsAspireHost, string? AspireHostingSdkVersion)> GetAppHostInformationAsync(FileInfo projectFile, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

        string[] cliArgs = ["msbuild", "-getproperty:IsAspireHost,AspireHostingSDKVersion", projectFile.FullName];

        // Syphon off the stdout from this execution to build the JSON document
        // containing the information we need for the apphost, but allow it to
        // flow up to the invoker if they have a callback.
        var stdoutBuilder = new StringBuilder();
        var existingStandardOutputCallback = options.StandardOutputCallback; // Preserve the existing callback if it exists.
        options.StandardOutputCallback = (line) => {
            stdoutBuilder.AppendLine(line);
            existingStandardOutputCallback?.Invoke(line);
        };

        var exitCode = await ExecuteAsync(
            args: cliArgs,
            env: null,
            workingDirectory: projectFile.Directory!,
            backchannelCompletionSource: null,
            options: options,
            cancellationToken: cancellationToken);

        if (exitCode == 0)
        {
            var stdout = stdoutBuilder.ToString();
            var json = JsonDocument.Parse(stdout);
            var properties = json.RootElement.GetProperty("Properties");

            if (!properties.TryGetProperty("IsAspireHost", out var isAspireHostElement))
            {
                return (exitCode, false, null);
            }

            if (isAspireHostElement.GetString() == "true")
            {
                if (properties.TryGetProperty("AspireHostingSDKVersion", out var aspireHostingSdkVersionElement))
                {
                    var aspireHostingSdkVersion = aspireHostingSdkVersionElement.GetString();
                    return (exitCode, true, aspireHostingSdkVersion);
                }
                else
                {
                    return (exitCode, true, null);
                }
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
        using var activity = _activitySource.StartActivity();
        
        string[] cliArgs = [
            "msbuild",
            $"-getProperty:{string.Join(",", properties)}",
            $"-getItem:{string.Join(",", items)}",
            projectFile.FullName
            ];

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
            workingDirectory: projectFile.Directory!,
            backchannelCompletionSource: null,
            options: options,
            cancellationToken);

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
        using var activity = _activitySource.StartActivity();

        if (watch && noBuild)
        {
            var ex = new InvalidOperationException("Cannot use --watch and --no-build at the same time.");
            backchannelCompletionSource?.SetException(ex);
            throw ex;
        }

        var watchOrRunCommand = watch ? "watch" : "run";
        var noBuildSwitch = noBuild ? "--no-build" : string.Empty;
        var noProfileSwitch = options.NoLaunchProfile ? "--no-launch-profile" : string.Empty;

        string[] cliArgs = [watchOrRunCommand, noBuildSwitch, noProfileSwitch, "--project", projectFile.FullName, "--", ..args];

        return await ExecuteAsync(
            args: cliArgs,
            env: env,
            workingDirectory: projectFile.Directory!,
            backchannelCompletionSource: backchannelCompletionSource,
            options: options,
            cancellationToken: cancellationToken);
    }

    public async Task<int> CheckHttpCertificateAsync(DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

        string[] cliArgs = ["dev-certs", "https", "--check", "--trust"];
        return await ExecuteAsync(
            args: cliArgs,
            env: null,
            workingDirectory: new DirectoryInfo(Environment.CurrentDirectory),
            backchannelCompletionSource: null,
            options: options,
            cancellationToken: cancellationToken);
    }

    public async Task<int> TrustHttpCertificateAsync(DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

        string[] cliArgs = ["dev-certs", "https", "--trust"];
        return await ExecuteAsync(
            args: cliArgs,
            env: null,
            workingDirectory: new DirectoryInfo(Environment.CurrentDirectory),
            backchannelCompletionSource: null,
            options: options,
            cancellationToken: cancellationToken);
    }

    public async Task<(int ExitCode, string? TemplateVersion)> InstallTemplateAsync(string packageName, string version, string? nugetSource, bool force, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity(nameof(InstallTemplateAsync), ActivityKind.Client);

        List<string> cliArgs = ["new", "install", $"{packageName}::{version}"];

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

        var exitCode = await ExecuteAsync(
            args: [.. cliArgs],
            env: null,
            workingDirectory: new DirectoryInfo(Environment.CurrentDirectory),
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
                throw new InvalidOperationException("Failed to parse template version from stdout.");
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

    public async Task<int> NewProjectAsync(string templateName, string name, string outputPath, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

        string[] cliArgs = ["new", templateName, "--name", name, "--output", outputPath];
        return await ExecuteAsync(
            args: cliArgs,
            env: null,
            workingDirectory: new DirectoryInfo(Environment.CurrentDirectory),
            backchannelCompletionSource: null,
            options: options,
            cancellationToken: cancellationToken);
    }

    internal static string GetBackchannelSocketPath()
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var dotnetCliPath = Path.Combine(homeDirectory, ".dotnet", "aspire", "cli", "backchannels");

        if (!Directory.Exists(dotnetCliPath))
        {
            Directory.CreateDirectory(dotnetCliPath);
        }

        var uniqueSocketPathSegment = Guid.NewGuid().ToString("N");
        var socketPath = Path.Combine(dotnetCliPath, $"cli.sock.{uniqueSocketPathSegment}");
        return socketPath;
    }

    public virtual async Task<int> ExecuteAsync(string[] args, IDictionary<string, string>? env, DirectoryInfo workingDirectory, TaskCompletionSource<IAppHostBackchannel>? backchannelCompletionSource, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

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
                options.StandardOutputCallback,
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

    private async Task StartBackchannelAsync(Process process, string socketPath, TaskCompletionSource<IAppHostBackchannel> backchannelCompletionSource, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

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
                logger.LogDebug("Connected to AppHost backchannel at {SocketPath}", socketPath);
                return;
            }
            catch (SocketException ex) when (process.HasExited && process.ExitCode != 0)
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
        using var activity = _activitySource.StartActivity();

        string[] cliArgs = ["build", projectFilePath.FullName];
        return await ExecuteAsync(
            args: cliArgs,
            env: null,
            workingDirectory: projectFilePath.Directory!,
            backchannelCompletionSource: null,
            options: options,
            cancellationToken: cancellationToken);
    }
    public async Task<int> AddPackageAsync(FileInfo projectFilePath, string packageName, string packageVersion, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

        string[] cliArgs = [
            "add",
            projectFilePath.FullName,
            "package",
            packageName,
            "--version",
            packageVersion
        ];

        logger.LogInformation("Adding package {PackageName} with version {PackageVersion} to project {ProjectFilePath}", packageName, packageVersion, projectFilePath.FullName);

        var result = await ExecuteAsync(
            args: cliArgs,
            env: null,
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

    public async Task<(int ExitCode, NuGetPackage[]? Packages)> SearchPackagesAsync(DirectoryInfo workingDirectory, string query, bool prerelease, int take, int skip, string? nugetSource, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();
        
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

        if (nugetSource is not null)
        {
            cliArgs.Add("--source");
            cliArgs.Add(nugetSource);
        }

        if (prerelease)
        {
            cliArgs.Add("--prerelease");
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

        var result = await ExecuteAsync(
            args: cliArgs.ToArray(),
            env: null,
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

            var foundPackages = new List<NuGetPackage>();
            try
            {
                using var document = JsonDocument.Parse(stdout);

                var searchResultsArray = document.RootElement.GetProperty("searchResult");

                foreach (var sourceResult in searchResultsArray.EnumerateArray())
                {
                    var source = sourceResult.GetProperty("sourceName").GetString();
                    var sourcePackagesArray = sourceResult.GetProperty("packages");

                    foreach (var packageResult in sourcePackagesArray.EnumerateArray())
                    {
                        var id = packageResult.GetProperty("id").GetString();

                        // var version = prerelease switch {
                        //     true => packageResult.GetProperty("version").GetString(),
                        //     false => packageResult.GetProperty("latestVersion").GetString()
                        // };

                        var version = packageResult.GetProperty("latestVersion").GetString();

                        foundPackages.Add(new NuGetPackage
                        {
                            Id = id!,
                            Version = version!,
                            Source = source!
                        });
                    }
                }
            }
            catch (JsonException ex)
            {
                logger.LogError($"Failed to read JSON returned by the package search. {ex.Message}");
                return (ExitCodeConstants.FailedToAddPackage, null);
            }

            return (result, foundPackages.ToArray());
        }
    }
}

internal class NuGetPackage
{
    public string Id { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}
