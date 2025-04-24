// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Text.RegularExpressions;

namespace Aspire.Templates.Tests;

public partial class AppHostTemplateTests : TemplateTestsBase
{
    public AppHostTemplateTests(ITestOutputHelper testOutput)
        : base(testOutput)
    {
    }

    [Fact]
    public async Task EnsureProjectsReferencing8_1_0AppHostWithNewerWorkloadCanBuild()
    {
        string projectId = "aspire-can-reference-8.1.0";
        await using var project = await AspireProject.CreateNewTemplateProjectAsync(
            projectId,
            "aspire-apphost",
            _testOutput,
            BuildEnvironment.ForDefaultFramework,
            addEndpointsHook: false);

        var projectPath = Path.Combine(project.RootDir, $"{projectId}.csproj");

        // Replace the reference to Aspire.Hosting.AppHost with version 8.1.0
        var newContents = AppHostPackageReferenceRegex().Replace(File.ReadAllText(projectPath), @"$1""8.1.0""");

        File.WriteAllText(projectPath, newContents);

        // Ensure project builds successfully
        await project.BuildAsync(workingDirectory: project.RootDir, token: TestContext.Current.CancellationToken);
    }

    [GeneratedRegex(@"(PackageReference\s.*""Aspire\.Hosting\.AppHost.*Version=)""[^""]+""")]
    private static partial Regex AppHostPackageReferenceRegex();
}
