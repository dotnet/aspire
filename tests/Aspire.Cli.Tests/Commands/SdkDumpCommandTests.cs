// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Tests.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.DotNet.RemoteExecutor;

namespace Aspire.Cli.Tests.Commands;

public class SdkDumpCommandTests(ITestOutputHelper outputHelper)
{
    private static readonly RemoteInvokeOptions s_remoteInvokeOptions = new()
    {
        StartInfo = { RedirectStandardOutput = true }
    };

    [Fact]
    public async Task SdkDumpWithHelpReturnsZero()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("sdk dump --help");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(0, exitCode);
    }

    [Theory]
    [InlineData("json")]
    [InlineData("Json")]
    [InlineData("JSON")]
    [InlineData("ci")]
    [InlineData("Ci")]
    [InlineData("pretty")]
    [InlineData("Pretty")]
    public void ParsesFormatOptionWithoutErrors(string format)
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse($"sdk dump --format {format}");

        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task SdkDumpWithNonexistentCsprojReturnsFailedToFindProject()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("sdk dump /nonexistent/path/to/integration.csproj");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(ExitCodeConstants.FailedToFindProject, exitCode);
    }

    [Fact]
    public async Task SdkDumpWithEmptyPackageNameReturnsInvalidCommand()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("sdk dump @13.2.0");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(ExitCodeConstants.InvalidCommand, exitCode);
    }

    [Fact]
    public async Task SdkDumpWithEmptyVersionReturnsInvalidCommand()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("sdk dump Aspire.Hosting.Redis@");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(ExitCodeConstants.InvalidCommand, exitCode);
    }

    [Fact]
    public async Task SdkDumpWithInvalidVersionFormatReturnsInvalidCommand()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("sdk dump Aspire.Hosting.Redis@not-a-version!!!");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(ExitCodeConstants.InvalidCommand, exitCode);
    }

    [Fact]
    public async Task SdkDumpWithInvalidArgumentFormatReturnsInvalidCommand()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("sdk dump some-random-string");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(ExitCodeConstants.InvalidCommand, exitCode);
    }

    [Fact]
    public void ParsesValidPackageFormatWithoutErrors()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("sdk dump Aspire.Hosting.Redis@13.2.0");

        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ParsesMultipleMixedArgumentsWithoutErrors()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("sdk dump Aspire.Hosting.Redis@13.2.0 Aspire.Hosting.PostgreSQL@13.2.0");

        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ParsesPreReleaseVersionWithoutErrors()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("sdk dump Aspire.Hosting.Redis@13.2.0-preview.1");

        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task SdkDumpWithDoubleAtSignReturnsInvalidCommand()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        // LastIndexOf('@') splits as "Aspire.Hosting.Redis@" and "13.2.0"
        // The @ in the package name part triggers format validation
        var result = command.Parse("sdk dump Aspire.Hosting.Redis@@13.2.0");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        // "Aspire.Hosting.Redis@" is not a valid semver, so it should fail version validation
        Assert.Equal(ExitCodeConstants.InvalidCommand, exitCode);
    }

    [Fact]
    public void SdkDumpCi_ForHostingProject_DoesNotEmitWarnings()
    {
        using var result = RemoteExecutor.Invoke(async (baseDirectory) =>
        {
            var repoRoot = FindRepoRoot(baseDirectory);
            var projectPath = Path.Combine(repoRoot, "src", "Aspire.Hosting", "Aspire.Hosting.csproj");
            Assert.True(File.Exists(projectPath), $"Could not find Aspire.Hosting project at '{projectPath}'.");

            var outputPath = Path.Combine(Path.GetTempPath(), "aspire-sdk-dump-tests", $"{Guid.NewGuid():N}.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            var previousCurrentDirectory = Environment.CurrentDirectory;
            var previousRepoRoot = Environment.GetEnvironmentVariable("ASPIRE_REPO_ROOT");
            var previousNoLogo = Environment.GetEnvironmentVariable(CliConfigNames.NoLogo);
            var previousDotNetTelemetry = Environment.GetEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT");
            var previousDotNetFirstTime = Environment.GetEnvironmentVariable("DOTNET_SKIP_FIRST_TIME_EXPERIENCE");
            var previousDotNetCertificate = Environment.GetEnvironmentVariable("DOTNET_GENERATE_ASPNET_CERTIFICATE");

            try
            {
                Environment.CurrentDirectory = repoRoot;
                Environment.SetEnvironmentVariable("ASPIRE_REPO_ROOT", repoRoot);
                Environment.SetEnvironmentVariable(CliConfigNames.NoLogo, "true");
                Environment.SetEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", "true");
                Environment.SetEnvironmentVariable("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "true");
                Environment.SetEnvironmentVariable("DOTNET_GENERATE_ASPNET_CERTIFICATE", "false");

                var exitCode = await Program.Main(["sdk", "dump", "--format", "ci", "--output", outputPath, projectPath]);
                Assert.Equal(ExitCodeConstants.Success, exitCode);

                var output = await File.ReadAllTextAsync(outputPath);
                var warningLines = output
                    .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => line.Contains("warning:", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                Console.WriteLine($"Warnings: {warningLines.Length}");
                foreach (var warningLine in warningLines)
                {
                    Console.WriteLine(warningLine);
                }

                Assert.Empty(warningLines);
            }
            finally
            {
                Environment.CurrentDirectory = previousCurrentDirectory;
                Environment.SetEnvironmentVariable("ASPIRE_REPO_ROOT", previousRepoRoot);
                Environment.SetEnvironmentVariable(CliConfigNames.NoLogo, previousNoLogo);
                Environment.SetEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", previousDotNetTelemetry);
                Environment.SetEnvironmentVariable("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", previousDotNetFirstTime);
                Environment.SetEnvironmentVariable("DOTNET_GENERATE_ASPNET_CERTIFICATE", previousDotNetCertificate);

                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }
            }
        }, AppContext.BaseDirectory, options: s_remoteInvokeOptions);

        outputHelper.WriteLine(result.Process.StandardOutput.ReadToEnd());
    }

    private static string FindRepoRoot(string startPath)
    {
        var currentDirectory = new DirectoryInfo(startPath);

        while (currentDirectory is not null)
        {
            if (File.Exists(Path.Combine(currentDirectory.FullName, "Aspire.slnx")))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new InvalidOperationException($"Could not find Aspire.slnx starting from '{startPath}'.");
    }
}
