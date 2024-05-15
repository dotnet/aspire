// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Workload.Tests;

public class PerTestFrameworkTemplatesTests : WorkloadTestsBase
{
    private static readonly XmlWriterSettings s_xmlWriterSettings = new() { ConformanceLevel = ConformanceLevel.Fragment };

    public PerTestFrameworkTemplatesTests(ITestOutputHelper testOutput) : base(testOutput)
    {
    }

    public static TheoryData<string, string> ProjectNamesWithTestTemplate_TestData()
    {
        var data = new TheoryData<string, string>();
        foreach (var testType in new[] { /*"aspire-mstest", "aspire-nunit", */ "aspire-xunit" })
        {
            foreach (var name in GetProjectNamesForTest())
            {
                data.Add(name, testType);
            }
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(ProjectNamesWithTestTemplate_TestData))]
    public async Task TemplatesForIndividualTestFrameworks(string prefix, string testTemplateName)
    {
        string id = $"{prefix}-{testTemplateName}";
        string config = "Debug";

        await using var project = await AspireProject.CreateNewTemplateProjectAsync(
            id,
            "aspire",
            _testOutput,
            buildEnvironment: BuildEnvironment.ForDefaultFramework);

        await project.BuildAsync(extraBuildArgs: [$"-c {config}"]).ConfigureAwait(false);
        await using (var context = await CreateNewBrowserContextAsync())
        {
            await AssertBasicTemplateAsync(context).ConfigureAwait(false);
        }

        // Add test project
        string testProjectName = $"{id}.{testTemplateName}Tests";
        using var newTestCmd = new DotNetCommand(_testOutput, label: $"new-test-{testTemplateName}")
                        .WithWorkingDirectory(project.RootDir);
        var res = await newTestCmd.ExecuteAsync($"new {testTemplateName} -o \"{testProjectName}\"");
        res.EnsureSuccessful();

        string testProjectDir = Path.Combine(project.RootDir, testProjectName);
        Assert.True(Directory.Exists(testProjectDir), $"Expected tests project at {testProjectDir}");

        string testProjectPath = Path.Combine(testProjectDir, testProjectName + ".csproj");
        Assert.True(File.Exists(testProjectPath), $"Expected tests project file at {testProjectPath}");

        PrepareTestCsFile(project.Id, testProjectDir, testTemplateName);
        PrepareTestProject(project, testProjectPath);

        Assert.True(Directory.Exists(testProjectDir), $"Expected tests project at {testProjectDir}");
        using var cmd = new DotNetCommand(_testOutput, label: $"test-{testTemplateName}")
                        .WithWorkingDirectory(testProjectDir)
                        .WithTimeout(TimeSpan.FromMinutes(3));

        res = await cmd.ExecuteAsync($"test -c {config}");

        Assert.True(res.ExitCode != 0, $"Expected the tests project build to fail");
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
            using (XmlWriter w = XmlWriter.Create(output, s_xmlWriterSettings))
            {
                w.WriteString(project.Id);
            }
            string xmlEncodedId = output.ToString();

            string projectReference = $@"<ProjectReference Include=""$(MSBuildThisFileDirectory)..\{xmlEncodedId}.AppHost\{xmlEncodedId}.AppHost.csproj"" />";

            string newContents = File.ReadAllText(projectPath)
                                    .Replace("</Project>", $"<ItemGroup>{projectReference}</ItemGroup>\n</Project>");
            File.WriteAllText(projectPath, newContents);
        }

        static void PrepareTestCsFile(string id, string projectDir, string testTemplateName)
        {
            string testCsPath = Path.Combine(projectDir, "IntegrationTest1.cs");
            var sb = new StringBuilder();

            // Uncomment everything after the marker line
            bool inTest = false;
            string marker = testTemplateName switch
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

                // FIXME: extract regex
                if (inTest && Regex.IsMatch(line, @"^\s*//"))
                {
                    sb.AppendLine(Regex.Replace(line, @"^\s*//", "    "));
                    continue;
                }

                sb.AppendLine(line);
            }

            // FIXME: add comment to keep the regex in sync with the targets, and template.json
            string classNameFromId = Regex.Replace(id, @"(((?<=\.)|^)(?=\d)|\W)", "_");
            sb.Replace("Projects.MyAspireApp_AppHost", $"Projects.{classNameFromId}_AppHost");
            File.WriteAllText(testCsPath, sb.ToString());
        }
    }
}
