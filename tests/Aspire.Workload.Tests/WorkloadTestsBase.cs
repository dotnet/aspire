// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    public static readonly string[] TestFrameworkTypes = ["none", "mstest", "nunit", "xunit.net"];

    public WorkloadTestsBase(ITestOutputHelper testOutput)
    {
        _testOutput = new TestOutputWrapper(testOutput);
    }

    private static IBrowser CreateBrowser()
    {
        var t = Task.Run(async () => await PlaywrightProvider.CreateBrowserAsync());

        // default timeout for playwright.Chromium.LaunchAsync is 30secs,
        // so using a timeout here as a fallback
        if (!t.Wait(45 * 1000))
        {
            throw new TimeoutException("Browser creation timed out");
        }

        return t.Result;
    }

    public static Task<IBrowserContext> CreateNewBrowserContextAsync()
        => PlaywrightProvider.HasPlaywrightSupport
                ? Browser.Value.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true })
                : throw new InvalidOperationException("Playwright is not available");

    protected Task<ResourceRow[]> CheckDashboardHasResourcesAsync(IPage dashboardPage, IEnumerable<ResourceRow> expectedResources, int timeoutSecs = 120)
        => CheckDashboardHasResourcesAsync(dashboardPage, expectedResources, _testOutput, timeoutSecs);

    protected static async Task<ResourceRow[]> CheckDashboardHasResourcesAsync(IPage dashboardPage, IEnumerable<ResourceRow> expectedResources, ITestOutputHelper testOutput, int timeoutSecs = 120)
    {
        // FIXME: check the page has 'Resources' label
        // fluent-toolbar/h1 resources

        testOutput.WriteLine($"Waiting for resources to appear on the dashboard");
        await Task.Delay(500);

        var expectedRowsTable = expectedResources.ToDictionary(r => r.Name);
        HashSet<string> foundNames = [];
        List<ResourceRow> foundRows = [];

        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSecs));

        while (foundNames.Count < expectedRowsTable.Count && !cts.IsCancellationRequested)
        {
            await Task.Delay(500);

            // _testOutput.WriteLine($"Checking for rows again");
            var rowsLocator = dashboardPage.Locator("//fluent-data-grid-row[@class='hover resource-row']");
            var allRows = await rowsLocator.AllAsync();
            // _testOutput.WriteLine($"found rows#: {allRows.Count}");
            if (allRows.Count == 0)
            {
                // Console.WriteLine ($"** no rows found ** elapsed: {sw.Elapsed.TotalSeconds} secs");
                continue;
            }

            foreach (var rowLoc in allRows)
            {
                // get the cells
                var cellLocs = await rowLoc.Locator("//fluent-data-grid-cell[@role='gridcell']").AllAsync();

                // is the resource name expected?
                var resourceName = await cellLocs[1].InnerTextAsync();
                if (!expectedRowsTable.TryGetValue(resourceName, out var expectedRow))
                {
                    Assert.Fail($"Row with unknown name found: {resourceName}");
                }
                if (foundNames.Contains(resourceName))
                {
                    continue;
                }

                AssertEqual(expectedRow.Name, resourceName, $"Name for {resourceName}");

                var actualState = await cellLocs[2].InnerTextAsync().ConfigureAwait(false);
                actualState = actualState.Trim();
                if (expectedRow.State != actualState && actualState != "Finished" && !actualState.Contains("failed", StringComparison.OrdinalIgnoreCase))
                {
                    testOutput.WriteLine($"[{expectedRow.Name}] expected state: '{expectedRow.State}', actual state: '{actualState}'");
                    continue;
                }
                AssertEqual(expectedRow.State, await cellLocs[2].InnerTextAsync(), $"State for {resourceName}");

                // Match endpoints

                var matchingEndpoints = 0;
                var expectedEndpoints = expectedRow.Endpoints;

                var endpointsFound =
                    (await rowLoc.Locator("//div[@class='fluent-overflow-item']").AllAsync())
                        .Select(async e => await e.InnerTextAsync())
                        .Select(t => t.Result.Trim(','))
                        .ToList();

                if (expectedEndpoints.Length != endpointsFound.Count)
                {
                    // _testOutput.WriteLine($"For resource '{resourceName}, found ")
                    // _testOutput.WriteLine($"-- expected: {expectedEndpoints.Length} found: {endpointsFound.Length}, expected: {string.Join(',', expectedEndpoints)} found: {string.Join(',', endpointsFound)} for {resourceName}");
                    continue;
                }

                AssertEqual(expectedEndpoints.Length, endpointsFound.Count, $"#endpoints for {resourceName}");

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

                foundRows.Add(expectedRow with { Endpoints = endpointsFound.ToArray() });
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
        if (!OperatingSystem.IsWindows())
        {
            // ActiveIssue for windows: https://github.com/dotnet/aspire/issues/4555
            yield return "aspire_龦唉丂荳_㐁ᠭ_ᠤསྲིདخەلꌠ_1ᥕ";
        }

        // ActiveIssue: https://github.com/dotnet/aspire/issues/4550
        // yield return "aspire  sta-rter.test"; // Issue: two spaces

        yield return "aspire_starter.1period then.34letters";
        yield return "aspire-starter & with.1";

        // ActiveIssue: https://github.com/dotnet/aspnetcore/issues/56277
        // yield return "aspire_😀";

        // basic case
        yield return "aspire";
    }

    public static async Task AssertStarterTemplateRunAsync(IBrowserContext? context, AspireProject project, string config, ITestOutputHelper _testOutput)
    {
        await project.StartAppHostAsync(extraArgs: [$"-c {config}"], noBuild: false);

        if (context is not null)
        {
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
        }
        else
        {
            _testOutput.WriteLine($"Skipping playwright part of the test");
        }

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
