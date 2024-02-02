// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Aspire.EndToEnd.Tests;

/// <summary>
/// This fixture ensures the TestProject.AppHost application is started before a test is executed.
/// 
/// Represents the the IntegrationServiceA project in the test application used to send HTTP requests
/// to the project's endpoints.
/// </summary>
public sealed class IntegrationServicesFixture : IAsyncLifetime
{
    private Process? _appHostProcess;
    private Dictionary<string, ProjectInfo>? _projects;

    public Dictionary<string, ProjectInfo> Projects => _projects!;

    public ProjectInfo IntegrationServiceA => Projects["integrationservicea"];

    public HttpClient HttpClient { get; } = new HttpClient();

    public async Task InitializeAsync()
    {
        var appHostDirectory = Path.Combine(GetRepoRoot(), "tests", "testproject", "TestProject.AppHost");

        var output = new StringBuilder();
        var appExited = new TaskCompletionSource();
        var appRunning = new TaskCompletionSource();
        var projectsParsed = new TaskCompletionSource();
        _appHostProcess = new Process();
        _appHostProcess.StartInfo = new ProcessStartInfo("dotnet", "run")
        {
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = appHostDirectory
        };
        _appHostProcess.OutputDataReceived += (sender, e) =>
        {
            output.AppendLine(e.Data);

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
        EventHandler appExitedCallback = (sender, e) => appExited.SetResult();
        _appHostProcess.Exited += appExitedCallback;

        _appHostProcess.Start();
        _appHostProcess.BeginOutputReadLine();

        var successfulTask = Task.WhenAll(appRunning.Task, projectsParsed.Task);
        var failedTask = appExited.Task;

        try
        {
            await Task.WhenAny(successfulTask, failedTask)
                .ContinueWith(t =>
                {
                    Assert.True(successfulTask == t.Result, $"App run failed: {Environment.NewLine}{output}");
                }, TaskScheduler.Default)
                .WaitAsync(TimeSpan.FromMinutes(5));
        }
        catch (TimeoutException)
        {
            Assert.Fail($"Running the TestProject.AppHost timed out: {Environment.NewLine}{output}");
        }

        _appHostProcess.Exited -= appExitedCallback;

        foreach (var project in Projects.Values)
        {
            project.Client = HttpClient;
        }
    }

    private static Dictionary<string, ProjectInfo> ParseProjectInfo(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, ProjectInfo>>(json)!;

    public async Task DisposeAsync()
    {
        if (_appHostProcess is not null)
        {
            _appHostProcess.StandardInput.WriteLine("Stop");
            await _appHostProcess.WaitForExitAsync();
        }
    }

    private static string GetRepoRoot()
    {
        var directory = AppContext.BaseDirectory;

        while (directory != null && !Directory.Exists(Path.Combine(directory, ".git")))
        {
            directory = Directory.GetParent(directory)!.FullName;
        }

        return directory!;
    }
}
