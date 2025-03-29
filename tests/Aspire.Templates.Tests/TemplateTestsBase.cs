// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Aspire.TestUtilities;
using Microsoft.Playwright;
using Xunit;
using static Aspire.Templates.Tests.TestExtensions;

namespace Aspire.Templates.Tests;

public partial class TemplateTestsBase
{
    [GeneratedRegex(@"^\s*//")]
    private static partial Regex CommentLineRegex();

    // Regex is from src/Aspire.Hosting.AppHost/build/Aspire.Hosting.AppHost.in.targets - _GeneratedClassNameFixupRegex
    [GeneratedRegex(@"(((?<=\.)|^)(?=\d)|\W)")]
    private static partial Regex GeneratedClassNameFixupRegex();
    private static Lazy<IBrowser> Browser => new(CreateBrowser);
    private static readonly XmlWriterSettings s_xmlWriterSettings = new() { ConformanceLevel = ConformanceLevel.Fragment };
    protected readonly TestOutputWrapper _testOutput;

    public static readonly string[] TestFrameworkTypes = ["none", "mstest", "nunit", "xunit.net"];

    public TemplateTestsBase(ITestOutputHelper testOutput)
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

    public async Task<string> CreateAndAddTestTemplateProjectAsync(
        string id,
        string testTemplateName,
        AspireProject project,
        TestTargetFramework? tfm = null,
        BuildEnvironment? buildEnvironment = null,
        Func<AspireProject, Task>? onBuildAspireProject = null)
    {
        buildEnvironment ??= BuildEnvironment.ForDefaultFramework;
        var tmfArg = tfm is not null ? $"-f {tfm.Value.ToTFMString()}" : "";

        // Add test project
        var testProjectName = $"{id}.{testTemplateName}Tests";
        using var newTestCmd = new DotNetNewCommand(
                                    _testOutput,
                                    label: $"new-test-{testTemplateName}",
                                    buildEnv: buildEnvironment)
                                .WithWorkingDirectory(project.RootDir);
        var res = await newTestCmd.ExecuteAsync($"{testTemplateName} {tmfArg} -o \"{testProjectName}\"");
        res.EnsureSuccessful();

        var testProjectDir = Path.Combine(project.RootDir, testProjectName);
        Assert.True(Directory.Exists(testProjectDir), $"Expected tests project at {testProjectDir}");

        var testProjectPath = Path.Combine(testProjectDir, testProjectName + ".csproj");
        Assert.True(File.Exists(testProjectPath), $"Expected tests project file at {testProjectPath}");

        PrepareTestCsFile(project.Id, testProjectDir, testTemplateName);
        PrepareTestProject(project, testProjectPath);

        return testProjectDir;

        static void PrepareTestProject(AspireProject project, string projectPath)
        {
            // Insert <ProjectReference Include="$(MSBuildThisFileDirectory)..\aspire-starter0.AppHost\aspire-starter0.AppHost.csproj" /> in the project file

            // taken from https://raw.githubusercontent.com/dotnet/templating/a325ffa18edd1590f9b340cf83d51d8eb567ebdc/src/Microsoft.TemplateEngine.Orchestrator.RunnableProjects/ValueForms/XmlEncodeValueFormFactory.cs
            StringBuilder output = new();
            using (var w = XmlWriter.Create(output, s_xmlWriterSettings))
            {
                w.WriteString(project.Id);
            }
            var xmlEncodedId = output.ToString();

            var projectReference = $@"<ProjectReference Include=""$(MSBuildThisFileDirectory)..\{xmlEncodedId}.AppHost\{xmlEncodedId}.AppHost.csproj"" />";

            var newContents = File.ReadAllText(projectPath)
                                    .Replace("</Project>", $"<ItemGroup>{projectReference}</ItemGroup>\n</Project>");
            File.WriteAllText(projectPath, newContents);
        }

        static void PrepareTestCsFile(string id, string projectDir, string testTemplateName)
        {
            var testCsPath = Path.Combine(projectDir, "IntegrationTest1.cs");
            var sb = new StringBuilder();

            // Uncomment everything after the marker line
            var inTest = false;
            var marker = testTemplateName switch
            {
                "aspire-nunit" or "aspire-nunit-9" => "// [Test]",
                "aspire-mstest" or "aspire-mstest-9" => "// [TestMethod]",
                "aspire-xunit" or "aspire-xunit-9" => "// [Fact]",
                _ => throw new NotImplementedException($"Unknown test template: {testTemplateName}")
            };

            foreach (var line in File.ReadAllLines(testCsPath))
            {
                if (!inTest && line.Contains(marker))
                {
                    inTest = true;
                }

                if (inTest && CommentLineRegex().IsMatch(line))
                {
                    sb.AppendLine(CommentLineRegex().Replace(line, "    "));
                    continue;
                }

                sb.AppendLine(line);
            }

            var classNameFromId = GeneratedClassNameFixupRegex().Replace(id, "_");
            sb.Replace("Projects.MyAspireApp_AppHost", $"Projects.{classNameFromId}_AppHost");
            File.WriteAllText(testCsPath, sb.ToString());
        }
    }

    public static Task<IBrowserContext> CreateNewBrowserContextAsync()
        => PlaywrightProvider.HasPlaywrightSupport
                ? Browser.Value.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true })
                : throw new InvalidOperationException("Playwright is not available");

    protected Task<ResourceRow[]> CheckDashboardHasResourcesAsync(IPage dashboardPage, IEnumerable<ResourceRow> expectedResources, string logPath, int timeoutSecs = 120)
        => CheckDashboardHasResourcesAsync(dashboardPage, expectedResources, _testOutput, logPath, timeoutSecs);

    protected static async Task<ResourceRow[]> CheckDashboardHasResourcesAsync(IPage dashboardPage,
                                                                               IEnumerable<ResourceRow> expectedResources,
                                                                               ITestOutputHelper testOutput,
                                                                               string logPath,
                                                                               int timeoutSecs = 120)
    {
        try
        {
            return await CheckDashboardHasResourcesActualAsync(dashboardPage, expectedResources, testOutput, timeoutSecs);
        }
        catch
        {
            string screenshotPath = Path.Combine(logPath, "dashboard-fail.png");
            await dashboardPage.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath });
            testOutput.WriteLine($"Dashboard screenshot saved to {screenshotPath}");
            throw;
        }
    }

    private static async Task<ResourceRow[]> CheckDashboardHasResourcesActualAsync(IPage dashboardPage, IEnumerable<ResourceRow> expectedResources, ITestOutputHelper testOutput, int timeoutSecs = 120)
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
            var rowsLocator = dashboardPage.Locator("//tr[@class='fluent-data-grid-row hover resource-row']");
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
                var cellLocs = await rowLoc.Locator("//td[@role='gridcell']").AllAsync();

                // is the resource name expected?
                var resourceNameCell = cellLocs[0];
                var resourceName = await resourceNameCell.InnerTextAsync();
                resourceName = resourceName.Trim();
                if (!expectedRowsTable.TryGetValue(resourceName, out var expectedRow))
                {
                    Assert.Fail($"Row with unknown name found: '{resourceName}'. Expected values: {string.Join(", ", expectedRowsTable.Keys.Select(k => $"'{k}'"))}");
                }
                if (foundNames.Contains(resourceName))
                {
                    continue;
                }

                AssertEqual(expectedRow.Name, resourceName, $"Name for '{resourceName}'");

                var stateCell = cellLocs[1];
                var actualState = await stateCell.InnerTextAsync().ConfigureAwait(false);
                actualState = actualState.Trim();
                if (expectedRow.State != actualState && actualState != "Finished" && !actualState.Contains("failed", StringComparison.OrdinalIgnoreCase))
                {
                    // testOutput.WriteLine($"[{expectedRow.Name}] expected state: '{expectedRow.State}', actual state: '{actualState}'");
                    continue;
                }
                AssertEqual(expectedRow.State, (await stateCell.InnerTextAsync()).Trim(), $"State for {resourceName}");

                // Match endpoints

                var matchingEndpoints = 0;
                var expectedEndpoints = expectedRow.Endpoints;

                var overflowItems = await rowLoc.Locator("//div[@class='fluent-overflow-item']").AllAsync();
                IEnumerable<ILocator> endpointsTextLocs;
                if (overflowItems.Count == 0)
                {
                    var tdItems = (await rowLoc.Locator("td").AllAsync()).ToArray();
                    endpointsTextLocs = [tdItems[5]];
                }
                else
                {
                    endpointsTextLocs = overflowItems;
                }
                var endpointsFound = endpointsTextLocs
                        .Select(async e => await e.InnerTextAsync())
                        .Select(t => t.Result.Trim(','))
                        .ToArray();

                if (expectedEndpoints.Length != endpointsFound.Length)
                {
                    // _testOutput.WriteLine($"For resource '{resourceName}, found ")
                    // _testOutput.WriteLine($"-- expected: {expectedEndpoints.Length} found: {endpointsFound.Length}, expected: {string.Join(',', expectedEndpoints)} found: {string.Join(',', endpointsFound)} for {resourceName}");
                    continue;
                }

                AssertEqual(expectedEndpoints.Length, endpointsFound.Length, $"#endpoints for {resourceName}");

                // endpointsFound: ["foo", "https://localhost:7589/"]
                foreach (var endpointFound in endpointsFound)
                {
                    // matchedEndpoints: ["https://localhost:7589/"]
                    string[] matchedEndpoints = expectedEndpoints.Where(e => Regex.IsMatch(endpointFound, e)).ToArray();
                    if (matchedEndpoints.Length == 0)
                    {
                        Assert.Fail($"Unexpected endpoint found: {endpointFound} for resource named {resourceName}. Expected endpoints: {string.Join(',', expectedEndpoints)}");
                    }
                    matchingEndpoints++;
                }

                AssertEqual(expectedEndpoints.Length, matchingEndpoints, $"Expected number of endpoints for {resourceName}");

                // Check 'Source' column
                var sourceCell = cellLocs[3];
                // Since this will be the entire command, we can just confirm that the path of the executable contains
                // the expected source (executable/project)
                Assert.Contains(expectedRow.SourceContains, await sourceCell.InnerTextAsync());

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
        => (prefix is null ? "" : $"{prefix}_") + FixupSymbolName(Path.GetRandomFileName());

    public static IEnumerable<string> GetProjectNamesForTest()
    {
        if (!PlatformDetection.IsRunningPRValidation)
        {
            // Avoid running these cases on PR validation

            if (!OperatingSystem.IsWindows())
            {
                // ActiveIssue for windows: https://github.com/dotnet/aspire/issues/4555
                yield return "aspire_龦唉丂荳_㐁ᠭ_ᠤསྲིདخەلꌠ_1ᥕ";
            }

            yield return "aspire_starter.1period then.34letters";
            yield return "aspire-starter & with.1";

            // ActiveIssue: https://github.com/dotnet/aspnetcore/issues/56277
            // yield return "aspire_😀";
        }

        // basic case
        yield return "aspire";
    }

    public static async Task AssertStarterTemplateRunAsync(IBrowserContext? context, AspireProject project, string config, ITestOutputHelper _testOutput)
    {
        await project.StartAppHostAsync(extraArgs: [$"-c {config}"], noBuild: false);

        if (context is not null)
        {
            var page = await project.OpenDashboardPageAsync(context);
            var resourceRows = await CheckDashboardHasResourcesAsync(
                page,
                StarterTemplateRunTestsBase<StarterTemplateFixture>.GetExpectedResources(project, hasRedisCache: false),
                _testOutput,
                project.LogPath).ConfigureAwait(false);

            string apiServiceUrl = resourceRows.First(r => r.Name == "apiservice").Endpoints[0];
            await StarterTemplateRunTestsBase<StarterTemplateFixture>.CheckApiServiceWorksAsync(apiServiceUrl, _testOutput, project.LogPath);

            string webFrontEnd = resourceRows.First(r => r.Name == "webfrontend").Endpoints[0];
            await StarterTemplateRunTestsBase<StarterTemplateFixture>.CheckWebFrontendWorksAsync(context, webFrontEnd, _testOutput, project.LogPath);
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

            // Build first, because `dotnet test` does not show test results if all the tests pass
            using var buildCmd = new DotNetCommand(testOutput, label: $"test-{testType}")
                                    .WithWorkingDirectory(testProjectDirectory)
                                    .WithTimeout(TimeSpan.FromSeconds(testRunTimeoutSecs));

            (await buildCmd.ExecuteAsync($"test -c {config}")).EnsureSuccessful();

            // .. then test with --no-build
            using var testCmd = new DotNetCommand(testOutput, label: $"test-{testType}")
                                    .WithWorkingDirectory(testProjectDirectory)
                                    .WithTimeout(TimeSpan.FromSeconds(testRunTimeoutSecs));

            var testRes = (await testCmd.ExecuteAsync($"test -c {config} --no-build"))
                                .EnsureSuccessful();

            Assert.Matches("Passed! * - Failed: *0, Passed: *1, Skipped: *0, Total: *1", testRes.Output);
            return testRes;
        }
    }

    public static TheoryData<string, TestSdk, TestTargetFramework, string?> TestDataForNewAndBuildTemplateTests(string templateName) => new()
        {
            // Previous Sdk, Previous TFM
            { templateName, TestSdk.Previous, TestTargetFramework.Previous, null },
            // Previous Sdk - Current TFM
            { templateName, TestSdk.Previous, TestTargetFramework.Current, "The current .NET SDK does not support targeting .NET 9.0" },

            // Current SDK, Previous TFM
            { templateName, TestSdk.Current, TestTargetFramework.Previous, null },
            // Current SDK, Current TFM - covered by other tests
            // { templateName, TestSdk.Current, TestTargetFramework.Current, null },

            // Current SDK + previous runtime, Previous TFM
            { templateName, TestSdk.CurrentSdkAndPreviousRuntime, TestTargetFramework.Previous, null },
            // Current SDK + previous runtime, Current TFM
            { templateName, TestSdk.CurrentSdkAndPreviousRuntime, TestTargetFramework.Current, null },
        };

    // Taken from dotnet/runtime src/tasks/Common/Utils.cs
    private static readonly char[] s_charsToReplace = new[] { '.', '-', '+', '<', '>' };
    public static string FixupSymbolName(string name)
    {
        UTF8Encoding utf8 = new();
        byte[] bytes = utf8.GetBytes(name);
        StringBuilder sb = new();

        foreach (byte b in bytes)
        {
            if ((b >= (byte)'0' && b <= (byte)'9') ||
                (b >= (byte)'a' && b <= (byte)'z') ||
                (b >= (byte)'A' && b <= (byte)'Z') ||
                (b == (byte)'_'))
            {
                sb.Append((char)b);
            }
            else if (s_charsToReplace.Contains((char)b))
            {
                sb.Append('_');
            }
            else
            {
                sb.Append($"_{b:X}_");
            }
        }

        return sb.ToString();
    }

}
