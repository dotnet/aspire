// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Playwright;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests.Helpers;

/// <summary>
/// Provides Playwright-based helpers for verifying the Aspire dashboard is functional
/// and showing expected resources in the correct state. Screenshots are saved to the
/// test results directory for CI artifact upload.
/// </summary>
internal static class DashboardVerificationHelpers
{
    private static bool s_browsersInstalled;
    private static readonly object s_installLock = new();

    /// <summary>
    /// Ensures Playwright Chromium browsers are installed. Safe to call multiple times;
    /// installation is performed only once per process.
    /// </summary>
    internal static void EnsureBrowsersInstalled()
    {
        if (s_browsersInstalled)
        {
            return;
        }

        lock (s_installLock)
        {
            if (s_browsersInstalled)
            {
                return;
            }

            var exitCode = Microsoft.Playwright.Program.Main(["install", "chromium", "--with-deps"]);
            if (exitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Playwright browser installation failed with exit code {exitCode}. " +
                    "Ensure the CI environment supports Playwright browser installation.");
            }

            s_browsersInstalled = true;
        }
    }

    /// <summary>
    /// Verifies the Aspire dashboard is accessible, displays the expected resources,
    /// and takes a screenshot for test artifacts.
    /// </summary>
    /// <param name="dashboardUrl">The full dashboard login URL (including auth token).</param>
    /// <param name="expectedResourceNames">Resource names that should appear in the dashboard.</param>
    /// <param name="screenshotPath">File path to save the dashboard screenshot.</param>
    /// <param name="output">Test output helper for logging.</param>
    /// <param name="timeout">Timeout for dashboard verification. Defaults to 60 seconds.</param>
    internal static async Task VerifyDashboardAsync(
        string dashboardUrl,
        IEnumerable<string> expectedResourceNames,
        string screenshotPath,
        ITestOutputHelper output,
        TimeSpan? timeout = null)
    {
        var effectiveTimeout = (float)(timeout ?? TimeSpan.FromSeconds(60)).TotalMilliseconds;

        EnsureBrowsersInstalled();

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
        });

        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(effectiveTimeout);

        output.WriteLine($"Navigating to dashboard: {dashboardUrl}");
        await page.GotoAsync(dashboardUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = effectiveTimeout,
        });

        // The login URL auto-authenticates via the token parameter and redirects to the
        // resources page. Wait for the resources table to render.
        output.WriteLine("Waiting for resources table to load...");
        await page.WaitForSelectorAsync(
            "fluent-data-grid, [class*='resource'], table",
            new PageWaitForSelectorOptions { Timeout = effectiveTimeout });

        // Give the dashboard a moment to finish rendering resource states
        await page.WaitForTimeoutAsync(3000);

        // Verify expected resources are present in the page content
        var pageContent = await page.ContentAsync();
        var missingResources = new List<string>();

        foreach (var resourceName in expectedResourceNames)
        {
            if (pageContent.Contains(resourceName, StringComparison.OrdinalIgnoreCase))
            {
                output.WriteLine($"  ✓ Found resource: {resourceName}");
            }
            else
            {
                output.WriteLine($"  ✗ Missing resource: {resourceName}");
                missingResources.Add(resourceName);
            }
        }

        // Take screenshot before asserting so we always get the artifact
        var screenshotDir = Path.GetDirectoryName(screenshotPath);
        if (screenshotDir is not null)
        {
            Directory.CreateDirectory(screenshotDir);
        }

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = screenshotPath,
            FullPage = true,
        });
        output.WriteLine($"Dashboard screenshot saved to: {screenshotPath}");

        Assert.Empty(missingResources);
    }

    /// <summary>
    /// Polls an HTTP endpoint from the host test process until it responds with the expected status code.
    /// Uses retry logic with exponential backoff.
    /// </summary>
    /// <param name="url">The URL to poll.</param>
    /// <param name="output">Test output helper for logging.</param>
    /// <param name="expectedStatusCode">Expected HTTP status code. Defaults to 200.</param>
    /// <param name="timeout">Overall timeout. Defaults to 60 seconds.</param>
    internal static async Task PollEndpointAsync(
        string url,
        ITestOutputHelper output,
        int expectedStatusCode = 200,
        TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(60);
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };

        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
        var deadline = DateTime.UtcNow + effectiveTimeout;
        var delay = TimeSpan.FromSeconds(2);
        var attempt = 0;

        output.WriteLine($"Polling endpoint: {url} (expecting {expectedStatusCode})");

        while (DateTime.UtcNow < deadline)
        {
            attempt++;
            try
            {
                var response = await client.GetAsync(url);
                var statusCode = (int)response.StatusCode;

                if (statusCode == expectedStatusCode)
                {
                    output.WriteLine($"  ✓ Endpoint responded with {statusCode} on attempt {attempt}");
                    return;
                }

                output.WriteLine($"  Attempt {attempt}: got {statusCode}, expected {expectedStatusCode}");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                output.WriteLine($"  Attempt {attempt}: {ex.GetType().Name} - {ex.Message}");
            }

            await Task.Delay(delay);
            delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 1.5, 10_000));
        }

        Assert.Fail($"Endpoint {url} did not respond with {expectedStatusCode} within {effectiveTimeout.TotalSeconds}s after {attempt} attempts");
    }

    /// <summary>
    /// Returns the standard path for dashboard screenshots within the test results directory.
    /// In CI, screenshots go under <c>$GITHUB_WORKSPACE/testresults/screenshots/</c> for artifact upload.
    /// Locally, they go to the system temp directory.
    /// </summary>
    /// <param name="testName">The name of the test, used as the screenshot filename.</param>
    /// <returns>The full path to save the screenshot.</returns>
    internal static string GetScreenshotPath(string testName)
    {
        var githubWorkspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
        string screenshotsDir;

        if (!string.IsNullOrEmpty(githubWorkspace))
        {
            screenshotsDir = Path.Combine(githubWorkspace, "testresults", "screenshots");
        }
        else
        {
            screenshotsDir = Path.Combine(Path.GetTempPath(), "aspire-cli-e2e", "screenshots");
        }

        Directory.CreateDirectory(screenshotsDir);
        return Path.Combine(screenshotsDir, $"{testName}.png");
    }
}
