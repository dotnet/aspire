// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Resources;
using Aspire.Cli.Templating;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using RootCommand = Aspire.Cli.Commands.RootCommand;

namespace Aspire.Cli.Tests.Commands;

public class NewCommandLocalTemplateTests(ITestOutputHelper outputHelper)
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LocalTemplate_PromptsFromEngineMetadataAndRenders(bool includeMessage)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await CreateTemplateAsync(workspace, "template");
        var interaction = new TestInteractionService();
        interaction.SetupStringPromptResponse("Welcome");
        interaction.SetupBooleanResponse(includeMessage);
        interaction.PromptForSelectionCallback = (prompt, choices, _, _) =>
        {
            Assert.Equal("Service kind", prompt);
            Assert.Equal(["api", "worker"], choices.Cast<string>());
            return "worker";
        };
        interaction.PromptForSelectionsCallback = (prompt, choices, _, _) =>
        {
            Assert.Equal("Features", prompt);
            Assert.Equal(["web", "worker"], choices.Cast<string>());
            return ["web", "worker"];
        };

        using var provider = CreateServices(workspace, interaction, interactive: true).BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();
        var exitCode = await command.Parse("new --template-path template --name Demo --output out --suppress-agent-init").InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, exitCode);
        Assert.Empty(interaction.DisplayedErrors);
        var textPrompt = Assert.Single(interaction.StringPromptCalls);
        Assert.Equal("Greeting text", textPrompt.PromptText);
        Assert.Equal("Hello", textPrompt.DefaultValue);
        Assert.Equal("Include message", Assert.Single(interaction.BooleanPromptCalls).PromptText);
        await Verify(ReadOutput(workspace)).ScrubInlineGuids().UseParameters(includeMessage);
    }

    [Fact]
    public async Task LocalTemplate_NonInteractiveUsesEngineDefaults()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await CreateTemplateAsync(workspace, "template");
        var interaction = new TestInteractionService();
        using var provider = CreateServices(workspace, interaction, interactive: false).BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var exitCode = await command.Parse("new --template-path template --name Demo --output out --suppress-agent-init").InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, exitCode);
        Assert.Empty(interaction.StringPromptCalls);
        Assert.Empty(interaction.BooleanPromptCalls);
        Assert.Empty(interaction.DisplayedErrors);
        await Verify(ReadOutput(workspace)).ScrubInlineGuids();
    }

    [Fact]
    public async Task LocalTemplate_UsesSymbolNamesWhenDisplayMetadataIsEmpty()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await CreateTemplateAsync(workspace, "template", displayNames: false);
        var interaction = new TestInteractionService();
        interaction.PromptForSelectionCallback = (prompt, choices, formatter, _) =>
        {
            Assert.Equal("Mode", prompt);
            Assert.Equal(["api", "worker"], choices.Cast<string>().Select(choice => formatter(choice)));
            return "api";
        };
        interaction.PromptForSelectionsCallback = (prompt, choices, formatter, _) =>
        {
            Assert.Equal("Features", prompt);
            Assert.Equal(["web", "worker"], choices.Cast<string>().Select(choice => formatter(choice)));
            return ["web"];
        };
        using var provider = CreateServices(workspace, interaction, interactive: true).BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();

        var exitCode = await command.Parse("new --template-path template --name Demo --output out --suppress-agent-init").InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, exitCode);
        Assert.Equal("Greeting", Assert.Single(interaction.StringPromptCalls).PromptText);
        Assert.Equal("IncludeMessage", Assert.Single(interaction.BooleanPromptCalls).PromptText);
    }

    [Fact]
    public async Task LocalTemplate_MissingRequiredParameterFailsBeforeWriting()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await CreateTemplateAsync(workspace, "template", requiredGreeting: true);
        var interaction = new TestInteractionService();
        using var provider = CreateServices(workspace, interaction, interactive: false).BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var exitCode = await command.Parse("new --template-path template --name Demo --output out --suppress-agent-init").InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.FailedToCreateNewProject, exitCode);
        Assert.False(Directory.Exists(Path.Combine(workspace.Path, "out")));
        Assert.Equal(string.Format(NewCommandStrings.LocalTemplateCreationFailed, "MissingMandatoryParam", "Greeting"), Assert.Single(interaction.DisplayedErrors));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("empty")]
    [InlineData("malformed")]
    [InlineData("file")]
    public async Task LocalTemplate_InvalidDirectoryReportsError(string kind)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        if (kind is "empty" or "malformed")
        {
            workspace.CreateDirectory("template");
        }
        if (kind == "malformed")
        {
            workspace.CreateDirectory("template/.template.config");
            await File.WriteAllTextAsync(Path.Combine(workspace.Path, "template", ".template.config", "template.json"), "{ not json");
        }
        if (kind == "file")
        {
            await File.WriteAllTextAsync(Path.Combine(workspace.Path, "template"), "not a directory");
        }
        var interaction = new TestInteractionService();
        using var provider = CreateServices(workspace, interaction, interactive: false).BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var exitCode = await command.Parse("new --template-path template --name Demo --output out --suppress-agent-init").InvokeAsync().DefaultTimeout();

        Assert.NotEqual(CliExitCodes.Success, exitCode);
        Assert.NotEmpty(interaction.DisplayedErrors);
        Assert.False(Directory.Exists(Path.Combine(workspace.Path, "out")));
    }

    [Theory]
    [InlineData("--source ./feed")]
    [InlineData("--version 1.0.0")]
    [InlineData("--channel daily")]
    [InlineData("--language csharp")]
    [InlineData("aspire-starter")]
    public async Task LocalTemplate_RejectsConflictingOptions(string options)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var interaction = new TestInteractionService();
        using var provider = CreateServices(workspace, interaction, interactive: false).BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var exitCode = await command.Parse($"new {options} --template-path template --suppress-agent-init").InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.InvalidCommand, exitCode);
        Assert.Equal(NewCommandStrings.TemplatePathConflictingOptions, Assert.Single(interaction.DisplayedErrors));
    }

    [Fact]
    public async Task LocalTemplate_DoesNotOverwriteNonEmptyOutput()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await CreateTemplateAsync(workspace, "template");
        workspace.CreateDirectory("out");
        await File.WriteAllTextAsync(Path.Combine(workspace.Path, "out", "existing.txt"), "Keep me");
        var interaction = new TestInteractionService();
        using var provider = CreateServices(workspace, interaction, interactive: false).BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var exitCode = await command.Parse("new --template-path template --name Demo --output out --suppress-agent-init").InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.FailedToCreateNewProject, exitCode);
        Assert.Single(interaction.DisplayedErrors);
        await Verify(ReadOutput(workspace));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LocalTemplate_PackageWithMultipleTemplatesRequiresSelection(bool interactive)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await CreateTemplateAsync(workspace, "templates/first");
        await CreateTemplateAsync(workspace, "templates/second");
        var interaction = new TestInteractionService();
        var services = CreateServices(workspace, interaction, interactive);
        services.AddSingleton<INewCommandPrompter>(new TestNewCommandPrompter(interaction)
        {
            PromptForTemplateCallback = templates =>
            {
                Assert.Equal(["first", "second"], templates.Select(template => template.Name));
                return templates[1];
            }
        });
        using var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var exitCode = await command.Parse("new --template-path templates --name Demo --output out --suppress-agent-init").InvokeAsync().DefaultTimeout();

        if (interactive)
        {
            Assert.Equal(CliExitCodes.Success, exitCode);
            Assert.Empty(interaction.DisplayedErrors);
            Assert.Equal("second", await File.ReadAllTextAsync(Path.Combine(workspace.Path, "out", "identity.txt")));
        }
        else
        {
            Assert.Equal(CliExitCodes.InvalidCommand, exitCode);
            Assert.Equal(NewCommandStrings.LocalTemplateSelectionRequiresInteraction, Assert.Single(interaction.DisplayedErrors));
            Assert.False(Directory.Exists(Path.Combine(workspace.Path, "out")));
        }
    }

    [Fact]
    public async Task LocalTemplate_RegistersHelpWithoutLoadingTheDirectory()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var interaction = new TestInteractionService();
        using var provider = CreateServices(workspace, interaction, interactive: false).BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();

        var exitCode = await command.Parse("new --template-path missing --help").InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, exitCode);
        var newCommand = provider.GetRequiredService<NewCommand>();
        Assert.Equal(NewCommandStrings.TemplatePathOptionDescription, Assert.Single(newCommand.Options, option => option.Name == "--template-path").Description);
        Assert.Empty(interaction.DisplayedErrors);
    }

    [Fact]
    public async Task LocalTemplate_CancellationDoesNotCreateOutput()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await CreateTemplateAsync(workspace, "template");
        var interaction = new TestInteractionService();
        using var provider = CreateServices(workspace, interaction, interactive: true).BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var parseResult = command.Parse("new --template-path template --name Demo --output out --suppress-agent-init");
        var factory = provider.GetRequiredService<LocalTemplateFactory>();
        var inputs = new TemplateInputs { Name = "Demo", Output = "out" };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            factory.CreateAsync("template", inputs, parseResult, new CancellationToken(canceled: true)));

        Assert.False(Directory.Exists(Path.Combine(workspace.Path, "out")));
    }

    private IServiceCollection CreateServices(TemporaryWorkspace workspace, TestInteractionService interaction, bool interactive)
    {
        return CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.InteractionServiceFactory = _ => interaction;
            options.CliHostEnvironmentFactory = _ => interactive
                ? TestHelpers.CreateInteractiveHostEnvironment()
                : TestHelpers.CreateNonInteractiveHostEnvironment();
            options.NewCommandPrompterFactory = _ => new NewCommandPrompter(interaction);
            options.DotNetSdkInstallerFactory = _ => new TestDotNetSdkInstaller
            {
                CheckAsyncCallback = _ => throw new InvalidOperationException("Local templates must not require the SDK.")
            };
            options.DotNetCliRunnerFactory = _ => new TestDotNetCliRunner
            {
                NewProjectAsyncCallback = (_, _, _, _, _) => throw new InvalidOperationException("Local templates must not invoke dotnet new.")
            };
            options.NuGetPackageCacheFactory = _ => new FakeNuGetPackageCache
            {
                GetTemplatePackagesAsyncCallback = (_, _, _, _) => throw new InvalidOperationException("Local templates must not query package feeds.")
            };
        });
    }

    private static Dictionary<string, string> ReadOutput(TemporaryWorkspace workspace)
    {
        var directory = Path.Combine(workspace.Path, "out");
        return Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .ToDictionary(path => Path.GetRelativePath(directory, path).Replace('\\', '/'), File.ReadAllText);
    }

    private static async Task CreateTemplateAsync(TemporaryWorkspace workspace, string directory, bool requiredGreeting = false, bool displayNames = true)
    {
        var root = workspace.CreateDirectory(directory);
        var name = root.Name;
        var config = root.CreateSubdirectory(".template.config");
        await File.WriteAllTextAsync(Path.Combine(config.FullName, "template.json"), $$"""
            {
              "$schema": "http://json.schemastore.org/template",
              "author": "Aspire tests",
              "identity": "Aspire.Local.{{name}}",
              "name": "{{name}}",
              "shortName": "{{name}}",
              "sourceName": "TemplateApp",
              "symbols": {
                "Greeting": {
                  "type": "parameter", "datatype": "string", "displayName": "{{(displayNames ? "Greeting text" : "")}}",
                  "defaultValue": "Hello", "isRequired": {{requiredGreeting.ToString().ToLowerInvariant()}}, "replaces": "GreetingValue"
                },
                "IncludeMessage": {
                  "type": "parameter", "datatype": "bool", "displayName": "{{(displayNames ? "Include message" : "")}}", "defaultValue": "true"
                },
                "Mode": {
                  "type": "parameter", "datatype": "choice", "displayName": "{{(displayNames ? "Service kind" : "")}}",
                  "defaultValue": "api", "replaces": "ModeValue",
                  "choices": [ { "choice": "worker" }, { "choice": "api" } ]
                },
                "Features": {
                  "type": "parameter", "datatype": "choice", "displayName": "{{(displayNames ? "Features" : "")}}",
                  "allowMultipleValues": true, "defaultValue": "web", "replaces": "FeaturesValue",
                  "choices": [ { "choice": "worker" }, { "choice": "web" } ]
                },
                "IsWorker": { "type": "computed", "value": "(Mode == \"worker\")" },
                "GeneratedId": { "type": "generated", "generator": "guid", "replaces": "GuidValue" }
              },
              "sources": [
                { "modifiers": [ { "condition": "(!IsWorker)", "exclude": [ "worker-only.txt" ] } ] }
              ],
              "postActions": [
                {
                  "actionId": "3A7C4B45-1F5D-4A30-959A-51B88E82B5D2",
                  "description": "Must not run",
                  "args": { "executable": "must-not-execute", "args": "" },
                  "manualInstructions": [ { "text": "Do not execute post-actions in this flow." } ]
                }
              ]
            }
            """);
        await File.WriteAllTextAsync(Path.Combine(root.FullName, "TemplateApp.cs"), """
            namespace TemplateApp;
            // GreetingValue
            // Mode: ModeValue
            // Features: FeaturesValue
            // Id: GuidValue
            #if (IncludeMessage)
            // Message included
            #endif
            #if (IsWorker)
            // Worker selected
            #endif
            """);
        await File.WriteAllTextAsync(Path.Combine(root.FullName, "worker-only.txt"), "Worker content");
        await File.WriteAllTextAsync(Path.Combine(root.FullName, "identity.txt"), name);
    }
}
