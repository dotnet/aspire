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
    private IBrowserContext? _context;

    /// <summary>
    /// Gets the directory where test artifacts (screenshots, traces, videos) are stored.
    /// </summary>
    public string ArtifactsDir { get; }

    public VsCodeWebFixture(IMessageSink messageSink)
    {
        _messageSink = messageSink;

        ArtifactsDir = Path.Combine(
            FindRepoRoot(),
            "artifacts",
            "testresults",
            "extension-e2e");

        Directory.CreateDirectory(ArtifactsDir);
    }

    /// <summary>
    /// Gets the base URL where VS Code is accessible.
    /// </summary>
    public string Url => _container?.Url
        ?? throw new InvalidOperationException("Container not started");

    public async ValueTask InitializeAsync()
    {
        var outputHelper = new MessageSinkOutputHelper(_messageSink);

        // Start VS Code in Docker
        _container = new VsCodeContainer(outputHelper);
        await _container.StartAsync();

        // Create Playwright browser
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
        });

        // Create a context with video recording
        _context = await _browser.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            RecordVideoDir = Path.Combine(ArtifactsDir, "videos"),
            RecordVideoSize = new RecordVideoSize { Width = 1920, Height = 1080 },
        });

        // Start tracing for the context
        await _context.Tracing.StartAsync(new()
        {
            Screenshots = true,
            Snapshots = true,
        });
    }

    /// <summary>
    /// Creates a new Playwright page navigated to the VS Code web UI.
    /// </summary>
    public async Task<IPage> CreatePageAsync()
    {
        if (_context is null)
        {
            throw new InvalidOperationException("Fixture not initialized");
        }

        var page = await _context.NewPageAsync();
        await page.GotoAsync(Url);

        return page;
    }

    /// <summary>
    /// Saves the current trace to a file.
    /// </summary>
    public async Task SaveTraceAsync(string testName)
    {
        if (_context is null)
        {
            return;
        }

        var tracePath = Path.Combine(ArtifactsDir, "traces", $"{testName}.zip");
        Directory.CreateDirectory(Path.GetDirectoryName(tracePath)!);

        await _context.Tracing.StopAsync(new()
        {
            Path = tracePath,
        });

        // Restart tracing for subsequent tests
        await _context.Tracing.StartAsync(new()
        {
            Screenshots = true,
            Snapshots = true,
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_context is not null)
        {
            try
            {
                await _context.Tracing.StopAsync(new()
                {
                    Path = Path.Combine(ArtifactsDir, "traces", "final.zip"),
                });
            }
            catch
            {
                // Best effort
            }

            await _context.CloseAsync();
        }

        if (_browser is not null)
        {
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
