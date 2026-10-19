// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;
using Aspire.TestUtilities;
using Xunit;

namespace Infrastructure.Tests;

public sealed class NativeCliBuildRootsTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    [RequiresTools(["pwsh"])]
    public async Task NativeOnlyOptimizationPreservesRestoreRoots(bool enabled, bool nativeOnly, bool bundleDependenciesOnly)
    {
        using var workspace = TemporaryWorkspace.Create(output);
        var root = workspace.CreateDirectory("repository with spaces").FullName;
        foreach (var relativePath in new[]
        {
            "src/Aspire.Cli/Aspire.Cli.csproj",
            "eng/clipack/Aspire.Cli.linux-x64.csproj",
            "eng/dcppack/Aspire.Hosting.Orchestration.linux-x64.csproj",
            "eng/dashboardpack/Aspire.Dashboard.Sdk.linux-x64.csproj"
        })
        {
            var path = Path.Combine(root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, "<Project />");
        }

        var project = Path.Combine(root, "roots.proj");
        new XDocument(new XElement("Project",
            new XElement("PropertyGroup",
                new XElement("RepoRoot", root + Path.DirectorySeparatorChar),
                new XElement("TargetRids", "linux-x64"),
                new XElement("SkipNativeCliBootstrapBuild", enabled),
                new XElement("SkipManagedBuild", nativeOnly),
                new XElement("BuildBundleDepsOnly", bundleDependenciesOnly),
                new XElement("SkipTestProjects", true),
                new XElement("SkipPlaygroundProjects", true)),
            new XElement("Import", new XAttribute("Project", Path.Combine(RepoRoot.Path, "eng", "Build.props"))),
            new XElement("Target", new XAttribute("Name", "Build"),
                new XAttribute("DependsOnTargets", "RecordRestore;ExecuteBuildTargets")),
            new XElement("Target", new XAttribute("Name", "RecordRestore"),
                new XElement("WriteLinesToFile",
                    new XAttribute("File", Path.Combine(root, "restore.txt")),
                    new XAttribute("Lines", "@(ProjectToBuild->'%(Filename)')"))),
            new XElement("Target", new XAttribute("Name", "ExecuteBuildTargets"),
                new XElement("WriteLinesToFile",
                    new XAttribute("File", Path.Combine(root, "build.txt")),
                    new XAttribute("Lines", "@(ProjectToBuild->'%(Filename)')"))))).Save(project);

        var runner = Path.Combine(root, "run.ps1");
        await File.WriteAllTextAsync(runner, """
            & $env:TEST_DOTNET msbuild $env:TEST_PROJECT -nologo -t:Build
            exit $LASTEXITCODE
            """);
        using var command = new PowerShellCommand(runner, output)
            .WithWorkingDirectory(root)
            .WithTimeout(TimeSpan.FromMinutes(1))
            .WithEnvironmentVariable("TEST_DOTNET", Path.Combine(RepoRoot.Path, ".dotnet", OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet"))
            .WithEnvironmentVariable("TEST_PROJECT", project);

        var result = await command.ExecuteAsync();
        result.EnsureSuccessful();

        string[] expectedRestore = bundleDependenciesOnly
            ? ["Aspire.Hosting.Orchestration.linux-x64", "Aspire.Dashboard.Sdk.linux-x64", "Aspire.Dashboard", "Aspire.TerminalHost"]
            : nativeOnly
                ? ["Aspire.Cli", "Aspire.Cli.linux-x64"]
                : ["OuterPreBuild", "Aspire.Cli", "Aspire.Hosting.Orchestration.linux-x64", "Aspire.Dashboard.Sdk.linux-x64", "Aspire.Cli.linux-x64"];
        Assert.Equal(expectedRestore, await File.ReadAllLinesAsync(Path.Combine(root, "restore.txt")));
        var expectedBuild = enabled && nativeOnly && !bundleDependenciesOnly
            ? ["Aspire.Cli.linux-x64"]
            : expectedRestore;
        Assert.Equal(expectedBuild, await File.ReadAllLinesAsync(Path.Combine(root, "build.txt")));
    }
}
