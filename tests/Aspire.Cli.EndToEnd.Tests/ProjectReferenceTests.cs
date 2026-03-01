// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end test for polyglot project reference support.
/// Creates a .NET hosting integration project and a TypeScript AppHost that references it
/// via settings.json, then verifies the integration is discovered and code-generated.
/// </summary>
public sealed class ProjectReferenceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task TypeScriptAppHostWithProjectReferenceIntegration()
    {
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        using var terminal = CliE2ETestHelpers.CreateTestTerminal();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        var waitingForAppHostCreated = new CellPatternSearcher()
            .Find("Created apphost.ts");

        var waitForStartSuccess = new CellPatternSearcher()
            .Find("AppHost started successfully.");

        // Pattern to verify our custom integration was code-generated
        var waitForAddMyServiceInCodegen = new CellPatternSearcher()
            .Find("addMyService");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            sequenceBuilder.InstallAspireBundleFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireBundleEnvironment(counter);
            sequenceBuilder.VerifyAspireCliVersion(commitSha, counter);
        }

        // Step 1: Capture the CLI version for use in the integration project
        sequenceBuilder
            .Type("ASPIRE_VER=$(aspire --version)")
            .Enter()
            .WaitForSuccessPrompt(counter)
            .Type("echo \"Aspire version: $ASPIRE_VER\"")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Step 2: Create the .NET hosting integration project
        sequenceBuilder
            .Type("mkdir -p MyIntegration")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Write the .csproj — uses the captured CLI version for the Aspire.Hosting package reference
        sequenceBuilder
            .Type("""cat > MyIntegration/MyIntegration.csproj << CSPROJ""")
            .Enter()
            .Type("""<Project Sdk="Microsoft.NET.Sdk">""")
            .Enter()
            .Type("  <PropertyGroup>")
            .Enter()
            .Type("    <TargetFramework>net10.0</TargetFramework>")
            .Enter()
            .Type("""    <NoWarn>\$(NoWarn);ASPIREATS001</NoWarn>""")
            .Enter()
            .Type("  </PropertyGroup>")
            .Enter()
            .Type("  <ItemGroup>")
            .Enter()
            .Type("""    <PackageReference Include="Aspire.Hosting" Version="$ASPIRE_VER" />""")
            .Enter()
            .Type("  </ItemGroup>")
            .Enter()
            .Type("</Project>")
            .Enter()
            .Type("CSPROJ")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Write the integration source code with [AspireExport]
        sequenceBuilder
            .Type("""cat > MyIntegration/MyIntegrationExtensions.cs << 'CS'""")
            .Enter()
            .Type("using Aspire.Hosting;")
            .Enter()
            .Type("using Aspire.Hosting.ApplicationModel;")
            .Enter()
            .Type("namespace Aspire.Hosting;")
            .Enter()
            .Type("public static class MyIntegrationExtensions")
            .Enter()
            .Type("{")
            .Enter()
            .Type("""    [AspireExport("addMyService")]""")
            .Enter()
            .Type("    public static IResourceBuilder<ContainerResource> AddMyService(")
            .Enter()
            .Type("        this IDistributedApplicationBuilder builder, string name)")
            .Enter()
            .Type("""        => builder.AddContainer(name, "myservice", "latest");""")
            .Enter()
            .Type("}")
            .Enter()
            .Type("CS")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Step 3: Create a TypeScript AppHost using aspire init
        sequenceBuilder
            .Type("aspire init --language typescript --non-interactive")
            .Enter()
            .WaitUntil(s => waitingForAppHostCreated.Search(s).Count > 0, TimeSpan.FromMinutes(2))
            .WaitForSuccessPrompt(counter);

        // Step 4: Update settings.json to add the project reference
        // Use jq to add the packages section with the project reference
        sequenceBuilder
            .Type("""jq '. + {"packages": {"MyIntegration": "../MyIntegration/MyIntegration.csproj"}}' .aspire/settings.json > .aspire/settings.tmp && mv .aspire/settings.tmp .aspire/settings.json""")
            .Enter()
            .WaitForSuccessPrompt(counter)
            .Type("cat .aspire/settings.json")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Step 5: Start the AppHost and verify it works
        sequenceBuilder
            .Type("aspire start --non-interactive")
            .Enter()
            .WaitUntil(s => waitForStartSuccess.Search(s).Count > 0, TimeSpan.FromMinutes(3))
            .WaitForSuccessPrompt(counter);

        // Step 6: Verify the custom integration was code-generated
        sequenceBuilder
            .Type("grep addMyService .modules/aspire.ts")
            .Enter()
            .WaitUntil(s => waitForAddMyServiceInCodegen.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .WaitForSuccessPrompt(counter);

        // Step 7: Clean up
        sequenceBuilder
            .Type("aspire stop --all 2>/dev/null || true")
            .Enter()
            .WaitForSuccessPrompt(counter)
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }
}
