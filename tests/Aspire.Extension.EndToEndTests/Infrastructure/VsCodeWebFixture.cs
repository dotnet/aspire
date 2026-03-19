// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Playwright;
using Xunit;
using Xunit.Sdk;

namespace Aspire.Extension.EndToEndTests.Infrastructure;

/// <summary>
/// xUnit class fixture that manages a Docker container running VS Code via
/// <c>code serve-web</c> and a Playwright browser connected to it.
/// </summary>
public sealed class VsCodeWebFixture : IAsyncLifetime
{
    private readonly IMessageSink _messageSink;
    private VsCodeContainer? _container;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    /// <summary>
    /// Gets the root directory for all test output.
    /// Each test gets its own subdirectory under this root via <see cref="CreateTestOutputDir"/>.
    /// </summary>
    public string OutputRoot { get; }

    public VsCodeWebFixture(IMessageSink messageSink)
    {
        _messageSink = messageSink;

        OutputRoot = Path.Combine(
            FindRepoRoot(),
            "artifacts",
            "testresults",
            "extension-e2e");

        Directory.CreateDirectory(OutputRoot);
    }

    /// <summary>
    /// Gets the base URL where VS Code is accessible.
    /// </summary>
    public string Url => _container?.Url
        ?? throw new InvalidOperationException("Container not started");

    /// <summary>
    /// Gets the locally-built Aspire artifacts, or <c>null</c> if no local build was detected.
    /// Tests requiring artifacts should skip when this is <c>null</c>.
    /// </summary>
    internal AspireBuildArtifacts? Artifacts { get; private set; }

    /// <summary>
    /// Gets the underlying container for running <c>docker exec</c> commands.
    /// </summary>
    internal VsCodeContainer Container => _container
        ?? throw new InvalidOperationException("Container not started");

    public async ValueTask InitializeAsync()
    {
        var outputHelper = new MessageSinkOutputHelper(_messageSink);

        // Detect locally-built Aspire artifacts (CLI, VSIX, NuGet packages).
        // When present, these are volume-mounted into the container for integration tests.
        var repoRoot = FindRepoRoot();
        Artifacts = AspireBuildArtifacts.Detect(repoRoot);

        if (Artifacts is not null)
        {
            outputHelper.WriteLine("Build artifacts detected — mounting CLI, VSIX, and NuGet packages");
        }
        else
        {
            outputHelper.WriteLine($"No build artifacts found. {AspireBuildArtifacts.DescribeMissing(repoRoot)}");
            outputHelper.WriteLine("Integration tests requiring local builds will be skipped.");
        }

        // Start VS Code in Docker (with artifacts + Docker socket if available)
        _container = new VsCodeContainer(outputHelper, Artifacts);
        await _container.StartAsync();

        // Create Playwright browser
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
        });
    }

    /// <summary>
    /// Creates a per-test output directory where all artifacts (screenshots, traces,
    /// recordings, videos) for a single test are stored together.
    /// </summary>
    public string CreateTestOutputDir(string testName)
    {
        var dir = Path.Combine(OutputRoot, testName);
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>
    /// Creates a new Playwright page navigated to the VS Code web UI.
    /// Video is recorded into the per-test output directory.
    /// </summary>
    /// <param name="testOutputDir">Directory for video and trace output.</param>
    /// <param name="folder">Optional container path to open as the workspace folder.</param>
    public async Task<IPage> CreatePageAsync(string testOutputDir, string? folder = null)
    {
        if (_browser is null)
        {
            throw new InvalidOperationException("Fixture not initialized");
        }

        // Each test gets its own browser context so videos and traces land in the test dir
        var context = await _browser.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            RecordVideoDir = testOutputDir,
            RecordVideoSize = new RecordVideoSize { Width = 1920, Height = 1080 },
        });

        await context.Tracing.StartAsync(new()
        {
            Screenshots = true,
            Snapshots = true,
        });

        var url = folder is not null ? $"{Url}/?folder={folder}" : Url;
        var page = await context.NewPageAsync();
        await page.GotoAsync(url);

        return page;
    }

    /// <summary>
    /// Saves the Playwright trace and closes the page's browser context.
    /// All artifacts (trace zip, video) are written into the per-test output directory.
    /// </summary>
    public async Task SaveTraceAsync(string testOutputDir, string testName)
    {
        // Find contexts that have pages (the test context)
        if (_browser is null)
        {
            return;
        }

        foreach (var context in _browser.Contexts)
        {
            try
            {
                var tracePath = Path.Combine(testOutputDir, $"{testName}.trace.zip");
                await context.Tracing.StopAsync(new()
                {
                    Path = tracePath,
                });
            }
            catch
            {
                // Best effort
            }

            // Close context — this finalizes video recording into the test dir
            await context.CloseAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        // Close any remaining browser contexts
        if (_browser is not null)
        {
            foreach (var context in _browser.Contexts)
            {
                try
                {
                    await context.CloseAsync();
                }
                catch
                {
                    // Best effort
                }
            }

            await _browser.CloseAsync();
        }

        _playwright?.Dispose();

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Aspire.slnx")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not find repo root");
    }

    /// <summary>
    /// Adapter to use <see cref="IMessageSink"/> as <see cref="ITestOutputHelper"/>
    /// for the container helper.
    /// </summary>
    private sealed class MessageSinkOutputHelper(IMessageSink sink) : ITestOutputHelper
    {
        public string Output => string.Empty;

        public void Write(string message)
        {
            sink.OnMessage(new DiagnosticMessage(message));
        }

        public void Write(string format, params object[] args)
        {
            sink.OnMessage(new DiagnosticMessage(string.Format(format, args)));
        }

        public void WriteLine(string message)
        {
            sink.OnMessage(new DiagnosticMessage(message));
        }

        public void WriteLine(string format, params object[] args)
        {
            sink.OnMessage(new DiagnosticMessage(string.Format(format, args)));
        }
    }
}
