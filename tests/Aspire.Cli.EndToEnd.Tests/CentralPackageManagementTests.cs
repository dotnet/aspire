// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for Central Package Management (CPM) compatibility.
/// Validates that aspire update correctly handles CPM projects.
/// Each test class runs as a separate CI job for parallelization.
/// </summary>
public sealed class CentralPackageManagementTests(ITestOutputHelper output)
{
    [Fact]
    public async Task AspireUpdateRemovesAppHostPackageVersionFromDirectoryPackagesProps()
    {
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        using var terminal = CliE2ETestHelpers.CreateTestTerminal();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // aspire update prompts
        var waitingForPerformUpdates = new CellPatternSearcher()
            .Find("Perform updates?");

        var waitingForNuGetConfigDirectory = new CellPatternSearcher()
            .Find("Which directory for NuGet.config file?");

        var waitingForApplyNuGetConfig = new CellPatternSearcher()
            .Find("Apply these changes to NuGet.config?");

        var waitingForUpdateSuccessful = new CellPatternSearcher()
            .Find("Update successful!");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireCliEnvironment(counter);
            sequenceBuilder.VerifyAspireCliVersion(commitSha, counter);
        }

        // Disable update notifications to prevent the CLI self-update prompt
        // from appearing after "Update successful!" and blocking the test.
        sequenceBuilder
            .Type("aspire config set features.updateNotificationsEnabled false -g")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Set up an old-format AppHost project with CPM that has a PackageVersion
        // for Aspire.Hosting.AppHost. This simulates a pre-migration project where
        // the user adopted CPM before the SDK started adding the implicit reference.
        var projectDir = Path.Combine(workspace.WorkspaceRoot.FullName, "CpmTest");
        var appHostDir = Path.Combine(projectDir, "CpmTest.AppHost");
        var appHostCsprojPath = Path.Combine(appHostDir, "CpmTest.AppHost.csproj");
        var directoryPackagesPropsPath = Path.Combine(projectDir, "Directory.Packages.props");

        sequenceBuilder
            .ExecuteCallback(() =>
            {
                Directory.CreateDirectory(appHostDir);

                File.WriteAllText(appHostCsprojPath, """
                    <Project Sdk="Microsoft.NET.Sdk">
                        <Sdk Name="Aspire.AppHost.Sdk" Version="9.1.0" />
                        <PropertyGroup>
                            <OutputType>Exe</OutputType>
                            <TargetFramework>net9.0</TargetFramework>
                            <IsAspireHost>true</IsAspireHost>
                        </PropertyGroup>
                        <ItemGroup>
                            <PackageReference Include="Aspire.Hosting.AppHost" />
                        </ItemGroup>
                    </Project>
                    """);

                File.WriteAllText(Path.Combine(appHostDir, "Program.cs"), """
                    var builder = DistributedApplication.CreateBuilder(args);
                    builder.Build().Run();
                    """);

                File.WriteAllText(directoryPackagesPropsPath, """
                    <Project>
                        <PropertyGroup>
                            <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
                        </PropertyGroup>
                        <ItemGroup>
                            <PackageVersion Include="Aspire.Hosting.AppHost" Version="9.1.0" />
                        </ItemGroup>
                    </Project>
                    """);
            })
            // Use --channel stable to skip the channel selection prompt that appears
            // in CI when PR hive directories are present.
            .Type($"aspire update --project \"{appHostCsprojPath}\" --channel stable")
            .Enter()
            .WaitUntil(s => waitingForPerformUpdates.Search(s).Count > 0, TimeSpan.FromSeconds(60))
            .Enter() // confirm "Perform updates?" (default: Yes)
            // The updater may prompt for a NuGet.config location and ask to apply changes
            // when the project doesn't have an existing NuGet.config. Accept defaults for both.
            .WaitUntil(s => waitingForNuGetConfigDirectory.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .Enter() // accept default directory
            .WaitUntil(s => waitingForApplyNuGetConfig.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .Enter() // confirm "Apply these changes to NuGet.config?" (default: Yes)
            .WaitUntil(s => waitingForUpdateSuccessful.Search(s).Count > 0, TimeSpan.FromSeconds(60))
            .WaitForSuccessPrompt(counter)
            // Verify the PackageVersion for Aspire.Hosting.AppHost was removed
            .VerifyFileDoesNotContain(directoryPackagesPropsPath, "Aspire.Hosting.AppHost")
            // Verify dotnet restore succeeds (would fail with NU1009 without the fix)
            .Type($"dotnet restore \"{appHostCsprojPath}\"")
            .Enter()
            .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(120))
            // Clean up: re-enable update notifications
            .Type("aspire config delete features.updateNotificationsEnabled -g")
            .Enter()
            .WaitForSuccessPrompt(counter)
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }
}
