// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end test for polyglot project reference support.
/// Creates a .NET hosting integration project and a TypeScript AppHost that references it
/// via settings.json, then verifies the integration is discovered, code-generated, and functional.
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

        // Pattern to verify the resource appears in describe output
        var waitForMyServiceInDescribe = new CellPatternSearcher()
            .Find("my-svc");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            sequenceBuilder.InstallAspireBundleFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireBundleEnvironment(counter);
            sequenceBuilder.VerifyAspireCliVersion(commitSha, counter);
        }

        // Step 1: Create a TypeScript AppHost (so we get the sdkVersion in settings.json)
        sequenceBuilder
            .Type("aspire init --language typescript --non-interactive")
            .Enter()
            .WaitUntil(s => waitingForAppHostCreated.Search(s).Count > 0, TimeSpan.FromMinutes(2))
            .WaitForSuccessPrompt(counter);

        // Step 2: Create the integration project, update settings.json, and modify apphost.ts
        sequenceBuilder.ExecuteCallback(() =>
        {
            var workDir = workspace.WorkspaceRoot.FullName;

            // Read the sdkVersion from the settings.json that aspire init created
            var settingsPath = Path.Combine(workDir, ".aspire", "settings.json");
            var settingsJson = File.ReadAllText(settingsPath);
            using var doc = JsonDocument.Parse(settingsJson);
            var sdkVersion = doc.RootElement.GetProperty("sdkVersion").GetString()!;

            // Create the .NET hosting integration project
            var integrationDir = Path.Combine(workDir, "MyIntegration");
            Directory.CreateDirectory(integrationDir);

            File.WriteAllText(Path.Combine(integrationDir, "MyIntegration.csproj"), $$"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <NoWarn>$(NoWarn);ASPIREATS001</NoWarn>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Aspire.Hosting" Version="{{sdkVersion}}" />
                  </ItemGroup>
                </Project>
                """);

            File.WriteAllText(Path.Combine(integrationDir, "MyIntegrationExtensions.cs"), """
                using Aspire.Hosting;
                using Aspire.Hosting.ApplicationModel;

                namespace Aspire.Hosting;

                public static class MyIntegrationExtensions
                {
                    [AspireExport("addMyService")]
                    public static IResourceBuilder<ContainerResource> AddMyService(
                        this IDistributedApplicationBuilder builder, string name)
                        => builder.AddContainer(name, "redis", "latest");
                }
                """);

            // Update settings.json to add the project reference
            using var settingsDoc = JsonDocument.Parse(settingsJson);
            var settings = new Dictionary<string, object>();
            foreach (var prop in settingsDoc.RootElement.EnumerateObject())
            {
                settings[prop.Name] = prop.Value.Clone();
            }
            settings["packages"] = new Dictionary<string, string>
            {
                ["MyIntegration"] = "../MyIntegration/MyIntegration.csproj"
            };

            var updatedJson = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsPath, updatedJson);

            // Update apphost.ts to use the custom integration
            File.WriteAllText(Path.Combine(workDir, "apphost.ts"), """
                import { createBuilder } from './.modules/aspire.js';

                const builder = await createBuilder();
                await builder.addMyService("my-svc");
                await builder.build().run();
                """);
        });

        // Step 3: Start the AppHost (triggers project ref build + codegen)
        sequenceBuilder
            .Type("aspire start --non-interactive")
            .Enter()
            .WaitUntil(s => waitForStartSuccess.Search(s).Count > 0, TimeSpan.FromMinutes(2))
            .WaitForSuccessPrompt(counter);

        // Step 4: Verify the custom integration was code-generated
        sequenceBuilder
            .Type("grep addMyService .modules/aspire.ts")
            .Enter()
            .WaitUntil(s => waitForAddMyServiceInCodegen.Search(s).Count > 0, TimeSpan.FromSeconds(5))
            .WaitForSuccessPrompt(counter);

        // Step 5: Wait for the custom resource to be up
        sequenceBuilder
            .Type("aspire wait my-svc --timeout 60")
            .Enter()
            .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(90));

        // Step 6: Verify the resource appears in describe
        sequenceBuilder
            .Type("aspire describe my-svc --format json > /tmp/my-svc-describe.json")
            .Enter()
            .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(15))
            .Type("cat /tmp/my-svc-describe.json")
            .Enter()
            .WaitUntil(s => waitForMyServiceInDescribe.Search(s).Count > 0, TimeSpan.FromSeconds(5))
            .WaitForSuccessPrompt(counter);

        // Step 7: Clean up
        sequenceBuilder
            .Type("aspire stop")
            .Enter()
            .WaitForSuccessPrompt(counter)
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }
}
