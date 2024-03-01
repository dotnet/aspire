// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using Aspire.TestProject;
using Aspire.Workload.Tests;

namespace Aspire.EndToEnd.Tests;

/// <summary>
/// This fixture ensures the TestProject.AppHost application is started before a test is executed.
///
/// Represents the the IntegrationServiceA project in the test application used to send HTTP requests
/// to the project's endpoints.
/// </summary>
public sealed class IntegrationServicesFixture : IAsyncLifetime
{
#if TESTS_RUNNING_OUTSIDE_OF_REPO
    public static bool TestsRunningOutsideOfRepo = true;
#else
    public static bool TestsRunningOutsideOfRepo;
#endif

    public Dictionary<string, ProjectInfo> Projects => _projects!;
    public BuildEnvironment BuildEnvironment { get; init; }
    public ProjectInfo IntegrationServiceA => Projects["integrationservicea"];

    private Process? _appHostProcess;
    private readonly TaskCompletionSource _appExited = new();
    private Dictionary<string, ProjectInfo>? _projects;
    private readonly IMessageSink _diagnosticMessageSink;
    private readonly TestOutputWrapper _testOutput;

    public IntegrationServicesFixture(IMessageSink diagnosticMessageSink)
    {
        _diagnosticMessageSink = diagnosticMessageSink;
        _testOutput = new TestOutputWrapper(messageSink: _diagnosticMessageSink);
        BuildEnvironment = new(TestsRunningOutsideOfRepo, (probePath, solutionRoot) =>
        {
            throw new InvalidProgramException(
                    $"Running outside-of-repo: Could not find {probePath} computed from solutionRoot={solutionRoot}. " +
                    $"Build all the packages with `./build -pack`. And install the sdk+workload 'dotnet build tests/Aspire.EndToEnd.Tests/Aspire.EndToEnd.csproj /t:InstallWorkloadUsingArtifacts /p:Configuration=<config>");
        });
        if (BuildEnvironment.HasSdkWithWorkload)
        {
            BuildEnvironment.EnvVars["TestsRunningOutsideOfRepo"] = "true";
        }
    }

    public async Task InitializeAsync()
    {
        var appHostDirectory = Path.Combine(BuildEnvironment.TestProjectPath, "TestProject.AppHost");
        if (TestsRunningOutsideOfRepo)
        {
            _testOutput.WriteLine("");
            _testOutput.WriteLine($"****************************************");
            _testOutput.WriteLine($"   Running tests outside-of-repo");
            _testOutput.WriteLine($"   TestProject: {appHostDirectory}");
            _testOutput.WriteLine($"   Using dotnet: {BuildEnvironment.DotNet}");
            _testOutput.WriteLine($"****************************************");
            _testOutput.WriteLine("");
        }

        {
            using var cmd = new DotNetCommand(BuildEnvironment, _testOutput, label: "build")
                .WithWorkingDirectory(appHostDirectory);

            (await cmd.ExecuteAsync(CancellationToken.None, $"build -bl:{Path.Combine(BuildEnvironment.LogRootPath, "testproject-build.binlog")} -v m"))
                .EnsureSuccessful();
        }

        var output = new StringBuilder();
        var projectsParsed = new TaskCompletionSource();
        var appRunning = new TaskCompletionSource();
        var stdoutComplete = new TaskCompletionSource();
        var stderrComplete = new TaskCompletionSource();
        _appHostProcess = new Process();

        string processArguments = $"run --no-build -- ";
        if (GetResourcesToSkip() is var resourcesToSkip && resourcesToSkip.Count > 0)
        {
            processArguments += $"--skip-resources {string.Join(',', resourcesToSkip)}";
        }
        _appHostProcess.StartInfo = new ProcessStartInfo(BuildEnvironment.DotNet, processArguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = appHostDirectory
        };

        foreach (var item in BuildEnvironment.EnvVars)
        {
            _appHostProcess.StartInfo.Environment[item.Key] = item.Value;
            _testOutput.WriteLine($"\t[{item.Key}] = {item.Value}");
        }

        _testOutput.WriteLine($"Starting the process: {BuildEnvironment.DotNet} {processArguments} in {_appHostProcess.StartInfo.WorkingDirectory}");
        _appHostProcess.OutputDataReceived += (sender, e) =>
        {
            if (e.Data is null)
            {
                stdoutComplete.SetResult();
                return;
            }

            output.AppendLine(e.Data);
            _testOutput.WriteLine($"[apphost] {e.Data}");

            if (e.Data?.StartsWith("$ENDPOINTS: ") == true)
            {
                _projects = ParseProjectInfo(e.Data.Substring("$ENDPOINTS: ".Length));
                projectsParsed.SetResult();
            }

            if (e.Data?.Contains("Distributed application started") == true)
            {
                appRunning.SetResult();
            }
        };
        _appHostProcess.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data is null)
            {
                stderrComplete.SetResult();
                return;
            }

            output.AppendLine(e.Data);
            _testOutput.WriteLine($"[apphost] {e.Data}");
        };

        EventHandler appExitedCallback = (sender, e) =>
        {
            _testOutput.WriteLine("");
            _testOutput.WriteLine($"----------- app has exited -------------");
            _testOutput.WriteLine("");
            _appExited.SetResult();
        };
        _appHostProcess.EnableRaisingEvents = true;
        _appHostProcess.Exited += appExitedCallback;

        _appHostProcess.EnableRaisingEvents = true;

        _appHostProcess.Start();
        _appHostProcess.BeginOutputReadLine();
        _appHostProcess.BeginErrorReadLine();

        var successfulTask = Task.WhenAll(appRunning.Task, projectsParsed.Task);
        var failedAppTask = _appExited.Task;
        var timeoutTask = Task.Delay(TimeSpan.FromMinutes(5));

        var resultTask = await Task.WhenAny(successfulTask, failedAppTask, timeoutTask);
        if (resultTask == failedAppTask)
        {
            // wait for all the output to be read
            var allOutputComplete = Task.WhenAll(stdoutComplete.Task, stderrComplete.Task);
            var appExitTimeout = Task.Delay(TimeSpan.FromSeconds(5));
            var t = await Task.WhenAny(allOutputComplete, appExitTimeout);
            if (t == appExitTimeout)
            {
                _testOutput.WriteLine($"\tand timed out waiting for the full output");
            }

            var outputMessage = output.ToString();
            var exceptionMessage = $"App run failed: {Environment.NewLine}{outputMessage}";
            if (outputMessage.Contains("docker was found but appears to be unhealthy", StringComparison.OrdinalIgnoreCase))
            {
                exceptionMessage = "Docker was found but appears to be unhealthy. " + exceptionMessage;
            }

            // should really fail and quit after this
            throw new ArgumentException(exceptionMessage);
        }
        Assert.True(resultTask == successfulTask, $"App run failed: {Environment.NewLine}{output}");

        var client = CreateHttpClient();
        foreach (var project in Projects.Values)
        {
            project.Client = client;
        }
    }

    private HttpClient CreateHttpClient()
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

                        _testOutput.WriteLine(msg);
                    };
                    options.Retry.MaxRetryAttempts = 20;
                });
            });

        return services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>().CreateClient();
    }

    private static Dictionary<string, ProjectInfo> ParseProjectInfo(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, ProjectInfo>>(json)!;

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
            using var cmd = new ToolCommand("docker", testOutput);
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

    public async Task DisposeAsync()
    {
        if (_appHostProcess is not null)
        {
            await DumpDockerInfoAsync(new TestOutputWrapper(null));

            if (!_appHostProcess.HasExited)
            {
                _appHostProcess.StandardInput.WriteLine("Stop");
            }
            await _appHostProcess.WaitForExitAsync();
        }
    }

    public void EnsureAppHostRunning()
    {
        if (_appHostProcess is null || _appHostProcess.HasExited || _appExited.Task.IsCompleted)
        {
            throw new InvalidOperationException("The app host process is not running.");
        }
    }

    private static IList<string> GetResourcesToSkip()
    {
        List<string> resourcesToSkip = new();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
        {
            resourcesToSkip.Add(nameof(TestResourceNames.cosmos));
        }
        if (BuildEnvironment.IsRunningOnCI)
        {
            resourcesToSkip.Add(nameof(TestResourceNames.cosmos));
            resourcesToSkip.Add(nameof(TestResourceNames.oracledatabase));
        }

        resourcesToSkip.Add(nameof(TestResourceNames.dashboard));

        return resourcesToSkip;
    }

}
