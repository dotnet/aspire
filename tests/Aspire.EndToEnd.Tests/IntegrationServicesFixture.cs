// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
// using Polly;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.EndToEnd.Tests;

/// <summary>
/// This fixture ensures the TestProject.AppHost application is started before a test is executed.
///
/// Represents the the IntegrationServiceA project in the test application used to send HTTP requests
/// to the project's endpoints.
/// </summary>
public sealed class IntegrationServicesFixture : IAsyncLifetime
{
    /* Set to true to force the testproject to run out-of-tree with the workload */
    public static bool ForceOutOfTree;// = false;

    public Dictionary<string, ProjectInfo> Projects => _projects!;
    public BuildEnvironment BuildEnvironment { get; } = new(ForceOutOfTree);
    public ProjectInfo IntegrationServiceA => Projects["integrationservicea"];

    private Process? _appHostProcess;
    private readonly TaskCompletionSource _appExited = new();
    private Dictionary<string, ProjectInfo>? _projects;
    private readonly IMessageSink _diagnosticMessageSink;
    private TestOutputWrapper? _testOutput;

    public IntegrationServicesFixture(IMessageSink messageSink)
    {
        _diagnosticMessageSink = messageSink;
    }

    public async Task InitializeAsync()
    {
        var appHostDirectory = Path.Combine(BuildEnvironment.TestProjectPath, "TestProject.AppHost");

        _testOutput = new TestOutputWrapper(messageSink: _diagnosticMessageSink);
        var output = new StringBuilder();
        var projectsParsed = new TaskCompletionSource();
        var appRunning = new TaskCompletionSource();
        var stdoutComplete = new TaskCompletionSource();
        var stderrComplete = new TaskCompletionSource();
        _appHostProcess = new Process();

        string processArguments = $"run -- ";
        if (GetComponentsToSkipArgument() is var componentsToSkip && componentsToSkip.Count > 0)
        {
            processArguments += $"--skip-components {string.Join(',', componentsToSkip)}";
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
            AddEnvironmentVariable(item.Key, item.Value);
        }
        if (ForceOutOfTree)
        {
            AddEnvironmentVariable("TestsRuningOutOfTree", "true");
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
            _testOutput.WriteLine($"[{DateTime.Now}][apphost] {e.Data}");

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
            _testOutput.WriteLine($"[{DateTime.Now}][apphost] {e.Data}");
        };

        EventHandler appExitedCallback = (sender, e) =>
        {
            _testOutput.WriteLine($"[{DateTime.Now}] ");
            _testOutput.WriteLine($"[{DateTime.Now}] ----------- app has exited -------------");
            _testOutput.WriteLine($"[{DateTime.Now}] ");
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
            // _testOutput.WriteLine($"resultTask == failedAppTask");
            // wait for all the output to be read
            var allOutputComplete = Task.WhenAll(stdoutComplete.Task, stderrComplete.Task);
            var appExitTimeout = Task.Delay(TimeSpan.FromSeconds(5));
            var t = await Task.WhenAny(allOutputComplete, appExitTimeout);
            if (t == appExitTimeout)
            {
                _testOutput.WriteLine($"\tand timed out waiting for the full output");
            }
            else
            {
                _testOutput.WriteLine($"\tall output completed");
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

        // FIXME: don't remove this.. fail the whole thing is the app exits early!
        //_appHostProcess.Exited -= appExitedCallback;

        var client = CreateHttpClient();
        foreach (var project in Projects.Values)
        {
            project.Client = client;
        }

        void AddEnvironmentVariable(string key, string? value)
        {
            if (value is not null)
            {
                _appHostProcess.StartInfo.Environment[key] = value;
                _testOutput.WriteLine($"\t[{key}] = {value}");
            }
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
                        string msg = $"[{DateTime.Now}] Retry #{args.AttemptNumber+1} for '{args.Outcome.Result?.RequestMessage?.RequestUri}'" +
                                        $" due to StatusCode: {(int?)args.Outcome.Result?.StatusCode} ReasonPhrase: '{args.Outcome.Result?.ReasonPhrase}'";

                        msg += (args.Outcome.Exception is not null) ? $" Exception: {args.Outcome.Exception.Message} " : "";
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

    private static IList<string> GetComponentsToSkipArgument()
    {
        List<string> componentsToSkip = new();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
        {
            componentsToSkip.Add("cosmos");
        }
        if (BuildEnvironment.IsRunningOnCI)
        {
            componentsToSkip.Add("cosmos");
            componentsToSkip.Add("oracledatabase");
        }

        componentsToSkip.Add("dashboard");

        return componentsToSkip;
    }

}
