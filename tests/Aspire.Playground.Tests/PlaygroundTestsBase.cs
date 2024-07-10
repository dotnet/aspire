// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Playwright;
// using Xunit;
using Xunit.Abstractions;
using Aspire.Workload.Tests;
using static Aspire.Workload.Tests.TestExtensions;
using System.Text.RegularExpressions;

namespace Aspire.Playground.Tests;

public class PlaygroundTestsBase
{
    private static Lazy<IBrowser> Browser => new(CreateBrowser);
    protected readonly TestOutputWrapper _testOutput;

    // public static readonly string[] TestFrameworkTypes = ["none", "mstest", "nunit", "xunit.net"];

    public PlaygroundTestsBase(ITestOutputHelper testOutput)
        => _testOutput = new TestOutputWrapper(testOutput);

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

#if true
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

                // ignore Source if null
                if (expectedRow.Source is not null)
                {
                    // Check 'Source' column
                    AssertEqual(expectedRow.Source, await cellLocs[4].InnerTextAsync(), $"Source for {resourceName}");
                }

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
#endif

    // Don't fixup the prefix so it can have characters meant for testing, like spaces
    public static string GetNewProjectId(string? prefix = null)
        => (prefix is null ? "" : $"{prefix}_") + Path.GetRandomFileName();
}
