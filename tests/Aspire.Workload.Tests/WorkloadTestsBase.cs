// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;
using static Aspire.Workload.Tests.TestExtensions;

namespace Aspire.Workload.Tests;

public class WorkloadTestsBase
{
    private static Lazy<IBrowser> Browser => new(CreateBrowser);
    protected readonly TestOutputWrapper _testOutput;

    public WorkloadTestsBase(ITestOutputHelper testOutput)
        => _testOutput = new TestOutputWrapper(testOutput);

    private static IBrowser CreateBrowser()
    {
        var t = Task.Run(async () =>
        {
            var playwright = await Playwright.CreateAsync();
            string? browserPath = EnvironmentVariables.BrowserPath;
            if (!string.IsNullOrEmpty(browserPath) && !File.Exists(browserPath))
            {
                throw new FileNotFoundException($"Browser path BROWSER_PATH='{browserPath}' does not exist");
            }

            BrowserTypeLaunchOptions options = new()
            {
                Headless = true,
                ExecutablePath = browserPath
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && string.IsNullOrEmpty(browserPath))
            {
                var probePath = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
                if (File.Exists(probePath))
                {
                    options.ExecutablePath = probePath;
                }
            }
            return await playwright.Chromium.LaunchAsync(options).ConfigureAwait(false);
        });

        // default timeout for playwright.Chromium.LaunchAsync is 30secs,
        // so using a timeout here as a fallback
        if (!t.Wait(45 * 1000))
        {
            throw new TimeoutException("Browser creation timed out");
        }

        return t.Result;
    }

    public static Task<IBrowserContext> CreateNewBrowserContextAsync()
        => Browser.Value.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true });

    protected Task<ResourceRow[]> CheckDashboardHasResourcesAsync(IPage dashboardPage, IEnumerable<ResourceRow> expectedResources, int timeoutSecs = 120)
        => CheckDashboardHasResourcesAsync(dashboardPage, expectedResources, _testOutput, timeoutSecs);

    protected static async Task<ResourceRow[]> CheckDashboardHasResourcesAsync(IPage dashboardPage, IEnumerable<ResourceRow> expectedResources, ITestOutputHelper testOutput, int timeoutSecs = 120)
    {
        // FIXME: check the page has 'Resources' label
        // fluent-toolbar/h1 resources

        testOutput.WriteLine($"Waiting for resources to appear on the dashboard");
        await Task.Delay(500);

        Dictionary<string, ResourceRow> expectedRowsTable = expectedResources.ToDictionary(r => r.Name);
        HashSet<string> foundNames = [];
        List<ResourceRow> foundRows = [];

        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSecs));

        while (foundNames.Count < expectedRowsTable.Count && !cts.IsCancellationRequested)
        {
            await Task.Delay(500);

            // _testOutput.WriteLine($"Checking for rows again");
            ILocator rowsLocator = dashboardPage.Locator("//fluent-data-grid-row[@class='resource-row']");
            var allRows = await rowsLocator.AllAsync();
            // _testOutput.WriteLine($"found rows#: {allRows.Count}");
            if (allRows.Count == 0)
            {
                // Console.WriteLine ($"** no rows found ** elapsed: {sw.Elapsed.TotalSeconds} secs");
                continue;
            }

            foreach (ILocator rowLoc in allRows)
            {
                // get the cells
                IReadOnlyList<ILocator> cellLocs = await rowLoc.Locator("//fluent-data-grid-cell[@role='gridcell']").AllAsync();
                Assert.Equal(8, cellLocs.Count);

                // is the resource name expected?
                string resourceName = await cellLocs[1].InnerTextAsync();
                if (!expectedRowsTable.TryGetValue(resourceName, out var expectedRow))
                {
                    Assert.Fail($"Row with unknown name found: {resourceName}");
                }
                if (foundNames.Contains(resourceName))
                {
                    continue;
                }

                string resourceNameInCell = await cellLocs[1].InnerTextAsync().ConfigureAwait(false);
                resourceNameInCell.Trim();
                AssertEqual(expectedRow.Name, resourceNameInCell, $"Name for {resourceName}");

                string actualState = await cellLocs[2].InnerTextAsync().ConfigureAwait(false);
                actualState = actualState.Trim();
                if (expectedRow.State != actualState && actualState != "Finished" && !actualState.Contains("failed", StringComparison.OrdinalIgnoreCase))
                {
                    testOutput.WriteLine($"[{expectedRow.Name}] expected state: '{expectedRow.State}', actual state: '{actualState}'");
                    continue;
                }
                AssertEqual(expectedRow.State, await cellLocs[2].InnerTextAsync(), $"State for {resourceName}");

                // Match endpoints

                int matchingEndpoints = 0;
                var expectedEndpoints = expectedRow.Endpoints;

                string[] endpointsFound =
                    (await rowLoc.Locator("//div[@class='fluent-overflow-item']").AllAsync())
                        .Select(async e => await e.InnerTextAsync())
                        .Select(t => t.Result.Trim(','))
                        .ToArray();
                // FIXME: this could still return "+2"
                if (endpointsFound.Length == 0)
                {
                    var cellText = await cellLocs[5].InnerTextAsync();
                    endpointsFound = cellText.Trim().Split(',', StringSplitOptions.RemoveEmptyEntries);
                }
                if (expectedEndpoints.Length != endpointsFound.Length)
                {
                    // _testOutput.WriteLine($"For resource '{resourceName}, found ")
                    // _testOutput.WriteLine($"-- expected: {expectedEndpoints.Length} found: {endpointsFound.Length}, expected: {string.Join(',', expectedEndpoints)} found: {string.Join(',', endpointsFound)} for {resourceName}");
                    continue;
                }

                AssertEqual(expectedEndpoints.Length, endpointsFound.Length, $"#endpoints for {resourceName}");

                // endpointsFound: ["foo", "https://localhost:7589/weatherforecast"]
                foreach (var endpointFound in endpointsFound)
                {
                    // matchedEndpoints: ["https://localhost:7589/weatherforecast"]
                    string[] matchedEndpoints = expectedEndpoints.Where(e => Regex.IsMatch(endpointFound, e)).ToArray();
                    if (matchedEndpoints.Length == 0)
                    {
                        Assert.Fail($"Unexpected endpoint found: {endpointFound} for resource named {resourceName}. Expected endpoints: {string.Join(',', expectedEndpoints)}");
                    }
                    matchingEndpoints++;
                }

                AssertEqual(expectedEndpoints.Length, matchingEndpoints, $"Expected number of endpoints for {resourceName}");

                // Check 'Source' column
                AssertEqual(expectedRow.Source, await cellLocs[4].InnerTextAsync(), $"Source for {resourceName}");

                foundRows.Add(expectedRow with { Endpoints = endpointsFound });
                foundNames.Add(resourceName);
            }
        }

        if (foundNames.Count != expectedRowsTable.Count)
        {
            Assert.Fail($"Expected rows not found: {string.Join(", ", expectedRowsTable.Keys.Except(foundNames))}");
        }

        return foundRows.ToArray();
    }

    // Don't fixup the prefix so it can have characters meant for testing, like spaces
    public static string GetNewProjectId(string? prefix = null)
        => (prefix is null ? "" : $"{prefix}_") + Path.GetRandomFileName();

    public static IEnumerable<string> GetProjectNamesForTest()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // FIXME: open an issue - fails to restore on windows
            // - https://helixre107v0xdeko0k025g8.blob.core.windows.net/dotnet-aspire-refs-pull-3270-merge-fc4fbed17c7744ecb5/Aspire.Workload.Tests.StarterTemplateProjectNamesTests/1/console.9ad1df43.log?helixlogtype=result
            yield return "aspire_龦唉丂荳_㐁ᠭ_ᠤསྲིདخەلꌠ_1ᥕ";
        }

        // "aspire  sta-rter.test", // Issue: two spaces
        //"aspire 龦唉丂荳◎℉-㐁&ᠭ.ᠤ སྲིད خەل ꌠ.1 ᥕ᧞", // with '&', and spaces

        // FIXME: these two fail on windows to restore..
        yield return "aspire_starter.1period then.34letters";
        yield return "aspire-starter & with.1";

        yield return "aspire";
    }

    public static async Task AssertStarterTemplateRunAsync(IBrowserContext context, AspireProject project, string config, ITestOutputHelper _testOutput)
    {
        await project.StartAppHostAsync(extraArgs: [$"-c {config}"], noBuild: false);

        var page = await project.OpenDashboardPageAsync(context);
        ResourceRow[] resourceRows;
        try
        {
            resourceRows = await CheckDashboardHasResourcesAsync(
                                    page,
                                    StarterTemplateRunTestsBase<StarterTemplateFixture>.GetExpectedResources(project, hasRedisCache: false),
                                    _testOutput).ConfigureAwait(false);
        }
        catch
        {
            string screenshotPath = Path.Combine(project.LogPath, "dashboard-fail.png");
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath });
            _testOutput.WriteLine($"Dashboard screenshot saved to {screenshotPath}");
            throw;
        }

        string url = resourceRows.First(r => r.Name == "webfrontend").Endpoints[0];
        await StarterTemplateRunTestsBase<StarterTemplateFixture>.CheckWebFrontendWorksAsync(context, url, _testOutput, project.LogPath);
        await project.StopAppHostAsync();
    }

    public static async Task<CommandResult?> AssertTestProjectRunAsync(string testProjectDirectory, string testType, ITestOutputHelper testOutput, string config = "Debug", int testRunTimeoutSecs = 3 * 60)
    {
        if (testType == "none")
        {
            Assert.False(Directory.Exists(testProjectDirectory), "Expected no tests project to be created");
            return null;
        }
        else
        {
            Assert.True(Directory.Exists(testProjectDirectory), $"Expected tests project at {testProjectDirectory}");
            using var cmd = new DotNetCommand(testOutput, label: $"test-{testType}")
                                    .WithWorkingDirectory(testProjectDirectory)
                                    .WithTimeout(TimeSpan.FromSeconds(testRunTimeoutSecs));

            var res = (await cmd.ExecuteAsync($"test -c {config}"))
                                .EnsureSuccessful();

            Assert.Matches("Passed! * - Failed: *0, Passed: *1, Skipped: *0, Total: *1", res.Output);
            return res;
        }
    }
}
