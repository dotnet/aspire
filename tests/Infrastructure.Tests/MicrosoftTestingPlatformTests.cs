// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using System.Text.Json;
using System.Xml.Linq;
using Aspire.TestUtilities;
using Xunit;

namespace Infrastructure.Tests;

public sealed class MicrosoftTestingPlatformTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(false, "20m")]
    [InlineData(true, "30m")]
    [RequiresTools(["pwsh"])]
    public async Task ProjectTimeoutOverridesFlowToMtpArguments(bool basicBuild, string expectedSessionTimeout)
    {
        using var workspace = TemporaryWorkspace.Create(output);
        var script = Path.Combine(workspace.Path, "evaluate.ps1");
        var resultPath = Path.Combine(workspace.Path, "evaluation_results.json");
        await File.WriteAllTextAsync(script, """
            [Console]::Error.WriteLine('MSBuild server unavailable: could not connect to the server within the timeout window; the server may have failed to start. Falling back to an in-process build.')
            & $env:TEST_DOTNET msbuild (Join-Path $env:TEST_REPO 'tests/Aspire.Templates.Tests/Aspire.Templates.Tests.csproj') -nologo `
              -getProperty:MtpBaseArgs,TestRunnerAdditionalArguments `
              "-getResultOutputFile:$env:TEST_RESULT" `
              "/p:RunOnlyBasicBuildTemplateTests=$env:TEST_BASIC_BUILD"
            [Console]::Out.WriteLine('MSBuild evaluation completed.')
            exit $LASTEXITCODE
            """);
        using var command = new PowerShellCommand(script, output)
            .WithWorkingDirectory(RepoRoot.Path)
            .WithEnvironmentVariable("TEST_DOTNET", Path.Combine(RepoRoot.Path, ".dotnet", OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet"))
            .WithEnvironmentVariable("TEST_REPO", RepoRoot.Path)
            .WithEnvironmentVariable("TEST_RESULT", resultPath)
            .WithEnvironmentVariable("TEST_BASIC_BUILD", basicBuild.ToString());
        var result = await command.ExecuteAsync();
        result.EnsureSuccessful();

        Assert.Contains("MSBuild server unavailable:", result.Output);
        Assert.Contains("MSBuild evaluation completed.", result.Output);

        // MSBuild server diagnostics can precede the JSON on the console; read only
        // the structured result file so diagnostics remain visible without corrupting it.
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(resultPath));
        var properties = document.RootElement.GetProperty("Properties");
        var expectedTimeoutArguments = $"--hangdump-timeout 15m --timeout {expectedSessionTimeout}";
        Assert.Contains(expectedTimeoutArguments, properties.GetProperty("MtpBaseArgs").GetString()!, StringComparison.Ordinal);
        var testRunnerAdditionalArguments = properties.GetProperty("TestRunnerAdditionalArguments").GetString()!;
        Assert.Equal(basicBuild, testRunnerAdditionalArguments.Contains("--filter-trait category=basic-build", StringComparison.Ordinal));
        Assert.Contains(expectedTimeoutArguments, testRunnerAdditionalArguments, StringComparison.Ordinal);
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task ArcadeRunnerOptionsAreRegisteredByTheCurrentTestHost()
    {
        using var workspace = TemporaryWorkspace.Create(output);
        var script = Path.Combine(workspace.Path, "runner-help.ps1");
        await File.WriteAllTextAsync(script, """
            & $env:TEST_DOTNET $env:TEST_ASSEMBLY --help
            exit $LASTEXITCODE
            """);
        using var command = new PowerShellCommand(script, output)
            .WithWorkingDirectory(RepoRoot.Path)
            .WithEnvironmentVariable("TEST_DOTNET", Path.Combine(RepoRoot.Path, ".dotnet", OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet"))
            .WithEnvironmentVariable("TEST_ASSEMBLY", typeof(MicrosoftTestingPlatformTests).Assembly.Location);
        var result = await command.ExecuteAsync();
        result.EnsureSuccessful();

        var target = XDocument.Load(Path.Combine(RepoRoot.Path, "eng", "Xunit3", "Microsoft.Testing.Platform.targets"));
        var arguments = string.Join(" ", target.Descendants("_TestRunnerArgs").Select(element => element.Value));
        var options = Regex.Matches(arguments, @"--[\w-]+").Select(match => match.Value).Distinct().ToArray();
        Assert.NotEmpty(options);
        foreach (var option in options)
        {
            Assert.Matches($@"(?m)^\s+{Regex.Escape(option)}\s*$", result.Output);
        }
    }
}
