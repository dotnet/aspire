// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Aspire.Workload.Tests;

public class AspireProject : IAsyncDisposable
{
    public static Lazy<HttpClient> Client => new(CreateHttpClient);
    public Process? AppHostProcess { get; private set; }
    public string Id { get; init; }
    public string RootDir { get; init; }
    public string LogPath { get; init; }
    public string AppHostProjectDirectory => Path.Combine(RootDir, $"{Id}.AppHost");
    public string ServiceDefaultsProjectPath => Path.Combine(RootDir, $"{Id}.ServiceDefaults");
    public string TestsProjectDirectory => Path.Combine(RootDir, $"{Id}.Tests");
    public Dictionary<string, ProjectInfo> InfoTable { get; private set; } = new(capacity: 0);
    public TaskCompletionSource AppExited { get; } = new();

    private readonly ITestOutputHelper _testOutput;
    private readonly BuildEnvironment _buildEnv;

    public AspireProject(string id, string baseDir, ITestOutputHelper testOutput, BuildEnvironment buildEnv)
    {
        Id = id;
        RootDir = baseDir;
        _testOutput = testOutput;
        _buildEnv = buildEnv;
        LogPath = Path.Combine(_buildEnv.LogRootPath, Id);
    }

    public async Task StartAsync(string[]? extraArgs = default, CancellationToken token = default, Action<ProcessStartInfo>? configureProcess = null)
    {
        object outputLock = new();
        var output = new StringBuilder();
        var projectsParsed = new TaskCompletionSource();
        var appRunning = new TaskCompletionSource();
        var stdoutComplete = new TaskCompletionSource();
        var stderrComplete = new TaskCompletionSource();
        AppHostProcess = new Process();

        var processArguments = $"run --no-build";
        processArguments += extraArgs is not null ? " " + string.Join(" ", extraArgs) : "";
        AppHostProcess.StartInfo = new ProcessStartInfo(_buildEnv.DotNet, processArguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = AppHostProjectDirectory
        };

        foreach (var item in _buildEnv.EnvVars)
        {
            AppHostProcess.StartInfo.Environment[item.Key] = item.Value;
            _testOutput.WriteLine($"\t[{item.Key}] = {item.Value}");
        }

        _testOutput.WriteLine($"Starting the process: {_buildEnv.DotNet} {processArguments} in {AppHostProcess.StartInfo.WorkingDirectory}");
        AppHostProcess.OutputDataReceived += (sender, e) =>
        {
            if (e.Data is null)
            {
                stdoutComplete.SetResult();
                return;
            }

            string line = e.Data;
            string logLine = $"[apphost] {line}";
            lock(outputLock)
            {
                output.AppendLine(logLine);
            }
            _testOutput.WriteLine(logLine);

            if (line?.StartsWith("$ENDPOINTS: ") == true)
            {
                InfoTable = ProjectInfo.Parse(line.Substring("$ENDPOINTS: ".Length));
                projectsParsed.SetResult();
            }

            if (line?.Contains("Distributed application started") == true)
            {
                appRunning.SetResult();
            }
        };
        AppHostProcess.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data is null)
            {
                stderrComplete.SetResult();
                return;
            }

            string line = $"[apphost] {e.Data}";
            lock(outputLock)
            {
                output.AppendLine(line);
            }
            _testOutput.WriteLine(line);
        };

        EventHandler appExitedCallback = (sender, e) =>
        {
            _testOutput.WriteLine("");
            _testOutput.WriteLine($"----------- [{Path.GetFileName(AppHostProjectDirectory)}] app has exited -------------");
            _testOutput.WriteLine("");
            AppExited.SetResult();
        };
        AppHostProcess.EnableRaisingEvents = true;
        AppHostProcess.Exited += appExitedCallback;

        AppHostProcess.EnableRaisingEvents = true;

        configureProcess?.Invoke(AppHostProcess.StartInfo);

        AppHostProcess.Start();
        AppHostProcess.BeginOutputReadLine();
        AppHostProcess.BeginErrorReadLine();

        var successfulTask = Task.WhenAll(appRunning.Task, projectsParsed.Task);
        var failedAppTask = AppExited.Task;
        var timeoutTask = Task.Delay(TimeSpan.FromMinutes(5), token);

        string outputMessage;
        // FIXME: cancellation token for the successfulTask?
        var resultTask = await Task.WhenAny(successfulTask, failedAppTask, timeoutTask);
        if (resultTask == failedAppTask)
        {
            // wait for all the output to be read
            var allOutputComplete = Task.WhenAll(stdoutComplete.Task, stderrComplete.Task);
            var appExitTimeout = Task.Delay(TimeSpan.FromSeconds(5), token);
            var t = await Task.WhenAny(allOutputComplete, appExitTimeout);
            if (t == appExitTimeout)
            {
                _testOutput.WriteLine($"\tand timed out waiting for the full output");
            }

            lock(outputLock)
            {
                outputMessage = output.ToString();
            }
            var exceptionMessage = $"App run failed: {Environment.NewLine}{outputMessage}";
            if (outputMessage.Contains("docker was found but appears to be unhealthy", StringComparison.OrdinalIgnoreCase))
            {
                exceptionMessage = "Docker was found but appears to be unhealthy. " + exceptionMessage;
            }

            // should really fail and quit after this
            throw new ArgumentException(exceptionMessage);
        }

        lock(outputLock)
        {
            outputMessage = output.ToString();
        }
        if (resultTask != successfulTask)
        {
            throw new InvalidOperationException($"App run failed: {Environment.NewLine}{outputMessage}");
        }

        foreach (var project in InfoTable.Values)
        {
            project.Client = Client.Value;
        }

        _testOutput.WriteLine($"-- Ready to run tests --");
    }

    public async Task BuildAsync(CancellationToken token = default)
    {
        using var restoreCmd = new DotNetCommand(_buildEnv, _testOutput, label: "restore")
                                    .WithWorkingDirectory(Path.Combine(RootDir, $"{Id}.AppHost"));
        var res = await restoreCmd.ExecuteAsync($"restore -bl:{Path.Combine(LogPath!, $"{Id}-restore.binlog")} /p:TreatWarningsAsErrors=true");
        res.EnsureSuccessful();

        using var buildCmd = new DotNetCommand(_buildEnv, _testOutput, label: "build")
                                        .WithWorkingDirectory(Path.Combine(RootDir, $"{Id}.AppHost"));
        res = await buildCmd.ExecuteAsync($"build -bl:{Path.Combine(LogPath!, $"{Id}-build.binlog")} /p:TreatWarningsAsErrors=true");
        res.EnsureSuccessful();
    }

    public async ValueTask DisposeAsync()
    {
        // TODO: check that everything shutdown
        if (AppHostProcess is null)
        {
            return;
        }

        await DumpDockerInfoAsync(new TestOutputWrapper(null));

        if (!AppHostProcess.HasExited)
        {
            AppHostProcess.StandardInput.WriteLine("Stop");
        }
        await AppHostProcess.WaitForExitAsync();
    }

    public async Task DumpDockerInfoAsync(ITestOutputHelper? testOutputArg = null)
    {
        var testOutput = testOutputArg ?? _testOutput!;
        testOutput.WriteLine("--------------------------- Docker info ---------------------------");

        using var cmd = new ToolCommand("docker", testOutput!, "container-list");
        (await cmd.ExecuteAsync(CancellationToken.None, $"container list --all"))
            .EnsureSuccessful();

        testOutput.WriteLine("--------------------------- Docker info (end) ---------------------------");
    }

    public async Task DumpComponentLogsAsync(string component, ITestOutputHelper? testOutputArg = null)
    {
        var testOutput = testOutputArg ?? _testOutput!;
        var cts = new CancellationTokenSource();

        string containerName;
        {
            using var cmd = new ToolCommand("docker", testOutput, label: "container-list");
            var res = (await cmd.ExecuteAsync(cts.Token, $"container list --all --filter name={component} --format {{{{.Names}}}}"))
                .EnsureSuccessful();
            containerName = res.Output;
        }

        if (string.IsNullOrEmpty(containerName))
        {
            testOutput.WriteLine($"No container found for {component}");
        }
        else
        {
            using var cmd = new ToolCommand("docker", testOutput, label: component);
            (await cmd.ExecuteAsync(cts.Token, $"container logs {containerName} -n 50"))
                .EnsureSuccessful();
        }
    }

    public void EnsureAppHostRunning()
    {
        if (AppHostProcess is null || AppHostProcess.HasExited || AppExited.Task.IsCompleted)
        {
            throw new InvalidOperationException("The app host process is not running.");
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var services = new ServiceCollection();
        services.AddHttpClient()
            .ConfigureHttpClientDefaults(b =>
            {
                b.ConfigureHttpClient(client =>
                {
                    // Disable the HttpClient timeout to allow the timeout strategies to control the timeout.
                    client.Timeout = Timeout.InfiniteTimeSpan;
                });

                b.UseSocketsHttpHandler((handler, sp) =>
                {
                    handler.PooledConnectionLifetime = TimeSpan.FromSeconds(5);
                    handler.ConnectTimeout = TimeSpan.FromSeconds(5);
                });

                // Ensure transient errors are retried for up to 5 minutes
                b.AddStandardResilienceHandler(options =>
                {
                    options.AttemptTimeout.Timeout = TimeSpan.FromMinutes(2);
                    options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(5); // needs to be at least double the AttemptTimeout to pass options validation
                    options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(10);
                    options.Retry.OnRetry = async (args) =>
                    {
                        var msg = $"Retry #{args.AttemptNumber+1} for '{args.Outcome.Result?.RequestMessage?.RequestUri}'" +
                                        $" due to StatusCode: {(int?)args.Outcome.Result?.StatusCode} ReasonPhrase: '{args.Outcome.Result?.ReasonPhrase}'";

                        msg += (args.Outcome.Exception is not null) ? $" Exception: {args.Outcome.Exception} " : "";
                        if (args.Outcome.Result?.Content is HttpContent content && (await content.ReadAsStringAsync()) is string contentStr)
                        {
                            msg += $" Content:{Environment.NewLine}{contentStr}";
                        }
                        Console.WriteLine(msg);
                    };
                    options.Retry.MaxRetryAttempts = 20;
                });
            });

        return services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>().CreateClient();
    }
}
