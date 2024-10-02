// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Workload.Tests;

public abstract partial class PerTestFrameworkTemplatesTests : WorkloadTestsBase
{
    private static readonly XmlWriterSettings s_xmlWriterSettings = new() { ConformanceLevel = ConformanceLevel.Fragment };

    [GeneratedRegex(@"^\s*//")]
    private static partial Regex CommentLineRegex();

    // Regex is from src/Aspire.Hosting.AppHost/build/Aspire.Hosting.AppHost.targets - _GeneratedClassNameFixupRegex
    [GeneratedRegex(@"(((?<=\.)|^)(?=\d)|\W)")]
    private static partial Regex GeneratedClassNameFixupRegex();

    private readonly string _testTemplateName;

    public PerTestFrameworkTemplatesTests(string testType, ITestOutputHelper testOutput) : base(testOutput)
    {
        _testTemplateName = testType;
    }

    public static IEnumerable<object[]> ProjectNamesWithTestTemplate_TestData()
    {
        foreach (var name in GetProjectNamesForTest())
        {
            yield return [name];
        }
    }

    [Theory]
    [MemberData(nameof(ProjectNamesWithTestTemplate_TestData))]
    public async Task TemplatesForIndividualTestFrameworks(string prefix)
    {
        var id = $"{prefix}-{_testTemplateName}";
        var config = "Debug";

        await using var project = await AspireProject.CreateNewTemplateProjectAsync(
            id,
            "aspire",
            _testOutput,
            buildEnvironment: BuildEnvironment.ForDefaultFramework);

        await project.BuildAsync(extraBuildArgs: [$"-c {config}"]);
        if (PlaywrightProvider.HasPlaywrightSupport)
        {
            await using (var context = await CreateNewBrowserContextAsync())
            {
                await AssertBasicTemplateAsync(context);
            }
        }

        // Add test project
        var testProjectName = $"{id}.{_testTemplateName}Tests";
        using var newTestCmd = new DotNetNewCommand(_testOutput, label: $"new-test-{_testTemplateName}")
                        .WithWorkingDirectory(project.RootDir);
        var res = await newTestCmd.ExecuteAsync($"{_testTemplateName} -o \"{testProjectName}\"");
        res.EnsureSuccessful();

        var testProjectDir = Path.Combine(project.RootDir, testProjectName);
        Assert.True(Directory.Exists(testProjectDir), $"Expected tests project at {testProjectDir}");

        var testProjectPath = Path.Combine(testProjectDir, testProjectName + ".csproj");
        Assert.True(File.Exists(testProjectPath), $"Expected tests project file at {testProjectPath}");

        PrepareTestCsFile(project.Id, testProjectDir, _testTemplateName);
        PrepareTestProject(project, testProjectPath);

        Assert.True(Directory.Exists(testProjectDir), $"Expected tests project at {testProjectDir}");
        using var cmd = new DotNetCommand(_testOutput, label: $"test-{_testTemplateName}")
                        .WithWorkingDirectory(testProjectDir)
                        .WithTimeout(TimeSpan.FromMinutes(3));

        res = await cmd.ExecuteAsync($"test -c {config}");

        Assert.True(res.ExitCode != 0, $"Expected the tests project run to fail");
        Assert.Matches("System.ArgumentException.*Resource 'webfrontend' not found.", res.Output);
        Assert.Matches("Failed! * - Failed: *1, Passed: *0, Skipped: *0, Total: *1", res.Output);

        async Task AssertBasicTemplateAsync(IBrowserContext context)
        {
            await project.StartAppHostAsync(extraArgs: [$"-c {config}"]);

            try
            {
                var page = await project.OpenDashboardPageAsync(context);
                await CheckDashboardHasResourcesAsync(page, []);
            }
            finally
            {
                await project.StopAppHostAsync();
            }
        }

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
                "aspire-nunit" => "// [Test]",
                "aspire-mstest" => "// [TestMethod]",
                "aspire-xunit" => "// [Fact]",
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
}

// Individual class for each test framework so the tests can run in separate helix jobs
public class MSTest_PerTestFrameworkTemplatesTests : PerTestFrameworkTemplatesTests
{
    public MSTest_PerTestFrameworkTemplatesTests(ITestOutputHelper testOutput) : base("aspire-mstest", testOutput)
    {
    }
}

public class Xunit_PerTestFrameworkTemplatesTests : PerTestFrameworkTemplatesTests
{
    public Xunit_PerTestFrameworkTemplatesTests(ITestOutputHelper testOutput) : base("aspire-xunit", testOutput)
    {
    }
}

public class Nunit_PerTestFrameworkTemplatesTests : PerTestFrameworkTemplatesTests
{
    public Nunit_PerTestFrameworkTemplatesTests(ITestOutputHelper testOutput) : base("aspire-nunit", testOutput)
    {
    }
}
