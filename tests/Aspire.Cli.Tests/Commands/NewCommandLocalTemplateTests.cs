// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Text.Json;
using Aspire.Cli.Commands;
using Aspire.Cli.DotNet;
using Aspire.Cli.Resources;
using Aspire.Cli.Templating;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Aspire.Cli.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
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

    [Theory]
    [InlineData(true, "AspireVersion", false)]
    [InlineData(false, "AspireVersion", false)]
    [InlineData(true, "AspireVersion", true)]
    [InlineData(false, "AspireVersion", true)]
    [InlineData(true, "aspireVersion", true)]
    [InlineData(false, "aspireVersion", true)]
    public async Task LocalTemplate_InjectsCurrentAspireVersionWithoutPrompting(bool interactive, string parameterName, bool hasDefault)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var config = workspace.CreateDirectory("template/.template.config");
        var defaultProperty = hasDefault ? "\"defaultValue\": \"1.0.0\"," : "";
        await File.WriteAllTextAsync(Path.Combine(config.FullName, "template.json"), $$"""
            {
              "identity": "Aspire.Local.Version",
              "name": "Versioned template",
              "shortName": "versioned",
              "symbols": {
                "{{parameterName}}": {
                  "type": "parameter",
                  "datatype": "string",
                  "isRequired": true,
                  {{defaultProperty}}
                  "replaces": "AspireVersionValue"
                }
              }
            }
            """);
        await File.WriteAllTextAsync(Path.Combine(workspace.Path, "template", "aspire.config.json"), """
            { "packages": { "Aspire.Hosting.JavaScript": "AspireVersionValue" } }
            """);

        const string packageVersion = "17.0.0-pr.12345.gabcdef12";
        var interaction = new TestInteractionService();
        var services = CreateServices(workspace, interaction, interactive);
        services.AddSingleton(workspace.CreateExecutionContext(
            identityChannel: "pr-12345",
            identityVersion: $"{packageVersion}+abcdef123456",
            identityOverridden: true));
        using var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();

        var exitCode = await command.Parse("new --template-path template --name Demo --output out --suppress-agent-init").InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, exitCode);
        Assert.Empty(interaction.DisplayedErrors);
        Assert.Empty(interaction.StringPromptCalls);
        using var generatedConfig = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(workspace.Path, "out", "aspire.config.json")));
        Assert.Equal(packageVersion, generatedConfig.RootElement.GetProperty("packages").GetProperty("Aspire.Hosting.JavaScript").GetString());
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

        var templateProvider = provider.GetRequiredService<ITemplateProvider>();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            templateProvider.GetTemplatesAsync(new TemplateSource("template", null), new CancellationToken(canceled: true)));

        Assert.False(Directory.Exists(Path.Combine(workspace.Path, "out")));
    }

    [Fact]
    public async Task LocalTemplate_IsDiscoveredThroughRegisteredFactories()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await CreateTemplateAsync(workspace, "template");
        var interaction = new TestInteractionService();
        using var provider = CreateServices(workspace, interaction, interactive: false).BuildServiceProvider();

        var engineFactory = Assert.Single(provider.GetServices<ITemplateFactory>().OfType<TemplateEngineFactory>());
        Assert.Empty(engineFactory.GetTemplates());
        Assert.Empty(await engineFactory.GetInitTemplatesAsync());
        Assert.Empty(await engineFactory.GetTemplatesAsync(null));
        var templates = await provider.GetRequiredService<ITemplateProvider>().GetTemplatesAsync(new TemplateSource("template", null));

        Assert.Equal("template", Assert.Single(templates).Name);
        Assert.False(Directory.Exists(Path.Combine(workspace.Path, "out")));
        Assert.Empty(interaction.StringPromptCalls);
        Assert.Empty(interaction.DisplayedErrors);
    }

    [Fact]
    public async Task LocalTemplate_AbsolutePathDoesNotCloneRepository()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await CreateTemplateAsync(workspace, "template");
        var interaction = new TestInteractionService();
        var processFactory = new TestProcessExecutionFactory();
        var services = CreateServices(workspace, interaction, interactive: false);
        services.AddSingleton<IProcessExecutionFactory>(processFactory);
        using var provider = services.BuildServiceProvider();

        var exitCode = await provider.GetRequiredService<RootCommand>().Parse(
            ["new", "--template-path", Path.Combine(workspace.Path, "template"), "--name", "Demo", "--output", "out", "--suppress-agent-init"])
            .InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, exitCode);
        Assert.Empty(processFactory.CreatedExecutions);
        Assert.Empty(interaction.DisplayedErrors);
        await Verify(ReadOutput(workspace)).ScrubInlineGuids();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RepositoryTemplate_ClonesGitRepositoryAndRenders(bool specifyTemplatePath)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await GitTestHelper.EnsureGitAvailableAsync(outputHelper);
        var repository = workspace.CreateDirectory("repository with spaces");
        var templateDirectory = specifyTemplatePath ? "repository with spaces/templates/first" : "repository with spaces";
        await CreateTemplateAsync(workspace, templateDirectory);
        await GitTestHelper.RunGitAsync(repository.FullName, outputHelper, "init");
        await GitTestHelper.ConfigureGitIdentityAsync(repository.FullName, outputHelper);
        await GitTestHelper.RunGitAsync(repository.FullName, outputHelper, "add", ".");
        await GitTestHelper.RunGitAsync(repository.FullName, outputHelper, "commit", "-m", "Add template");

        var interaction = new TestInteractionService();
        var services = CreateServices(workspace, interaction, interactive: false);
        services.AddSingleton<IProcessExecutionFactory>(new ProcessExecutionFactory(new HostEnvironment(), NullLogger<ProcessExecutionFactory>.Instance));
        var cacheRoot = Path.Combine(workspace.WorkspaceRoot.FullName, ".aspire", "cache", "templates");
        using (var provider = services.BuildServiceProvider())
        {
            List<string> arguments = ["new", "--template-repository", new Uri(repository.FullName).AbsoluteUri,
                "--name", "Demo", "--output", "out", "--suppress-agent-init"];
            if (specifyTemplatePath)
            {
                arguments.AddRange(["--template-path", "templates/first"]);
            }

            var exitCode = await provider.GetRequiredService<RootCommand>().Parse(arguments.ToArray()).InvokeAsync().DefaultTimeout();

            Assert.Equal(CliExitCodes.Success, exitCode);
            Assert.Empty(interaction.DisplayedErrors);
            Assert.Empty(interaction.StringPromptCalls);
            var checkout = Assert.Single(Directory.GetDirectories(cacheRoot));
            Assert.True(Directory.Exists(Path.Combine(checkout, ".git")));
            await Verify(ReadOutput(workspace)).ScrubInlineGuids().UseParameters(specifyTemplatePath);
        }

        Assert.Empty(Directory.GetFileSystemEntries(cacheRoot));
        Assert.True(Directory.Exists(repository.FullName));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RepositoryTemplate_CloneFailureIsReportedAndCleanedUp(bool gitMissing)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var interaction = new TestInteractionService();
        var processFactory = new TestProcessExecutionFactory
        {
            AttemptCallback = (_, options) =>
            {
                options.StandardErrorCallback?.Invoke("fatal: repository not found");
                return (128, null);
            }
        };
        if (gitMissing)
        {
            processFactory.CreateExecutionWithFileNameCallback = (_, _, _, _, _) => throw new Win32Exception(2);
        }

        var services = CreateServices(workspace, interaction, interactive: false);
        services.AddSingleton<IProcessExecutionFactory>(processFactory);
        using (var provider = services.BuildServiceProvider())
        {
            var exitCode = await provider.GetRequiredService<RootCommand>()
                .Parse("new --template-repository https://example.test/templates.git --template-path template --name Demo --output out --suppress-agent-init")
                .InvokeAsync().DefaultTimeout();

            Assert.Equal(CliExitCodes.FailedToInstallTemplates, exitCode);
            var expectedError = gitMissing
                ? string.Format(NewCommandStrings.TemplateRepositoryCloneFailed, new Win32Exception(2).Message)
                : string.Format(NewCommandStrings.TemplateRepositoryCloneExited, 128, "fatal: repository not found");
            Assert.Equal(expectedError, Assert.Single(interaction.DisplayedErrors));
            Assert.Equal(1, processFactory.AttemptCount);
            Assert.False(Directory.Exists(Path.Combine(workspace.Path, "out")));
        }

        Assert.Empty(Directory.GetFileSystemEntries(Path.Combine(workspace.WorkspaceRoot.FullName, ".aspire", "cache", "templates")));
    }

    [Theory]
    [InlineData("..")]
    [InlineData("../outside")]
    [InlineData("templates/../../../outside")]
    [InlineData("absolute")]
    public async Task RepositoryTemplate_RejectsPathsOutsideRepositoryBeforeCloning(string templatePath)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var interaction = new TestInteractionService();
        var processFactory = new TestProcessExecutionFactory();
        var services = CreateServices(workspace, interaction, interactive: false);
        services.AddSingleton<IProcessExecutionFactory>(processFactory);
        using (var provider = services.BuildServiceProvider())
        {
            var exitCode = await provider.GetRequiredService<RootCommand>().Parse(
                ["new", "--template-repository", "https://example.test/templates.git", "--template-path",
                    templatePath == "absolute" ? workspace.Path : templatePath,
                    "--name", "Demo", "--output", "out", "--suppress-agent-init"]).InvokeAsync().DefaultTimeout();

            Assert.Equal(CliExitCodes.InvalidCommand, exitCode);
            Assert.Equal(NewCommandStrings.TemplateRepositoryPathInvalid, Assert.Single(interaction.DisplayedErrors));
            Assert.Empty(processFactory.CreatedExecutions);
            Assert.False(Directory.Exists(Path.Combine(workspace.Path, "out")));
        }

        Assert.Empty(Directory.GetFileSystemEntries(Path.Combine(workspace.WorkspaceRoot.FullName, ".aspire", "cache", "templates")));
    }

    [Fact]
    public async Task RepositoryTemplate_RejectsTemplatePathThroughSymbolicLink()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var interaction = new TestInteractionService();
        var processFactory = new TestProcessExecutionFactory
        {
            FileNameAssertionCallback = (_, arguments, _, _, _) =>
            {
                Directory.CreateDirectory(Path.Combine(arguments[^1], ".git"));
                try
                {
                    Directory.CreateSymbolicLink(Path.Combine(arguments[^1], "template"), workspace.Path);
                }
                catch (UnauthorizedAccessException)
                {
                    Assert.Skip("Creating symbolic links requires additional privileges on this platform.");
                }
            }
        };
        var services = CreateServices(workspace, interaction, interactive: false);
        services.AddSingleton<IProcessExecutionFactory>(processFactory);
        using (var provider = services.BuildServiceProvider())
        {
            var exitCode = await provider.GetRequiredService<RootCommand>()
                .Parse("new --template-repository https://example.test/templates.git --template-path template --name Demo --output out --suppress-agent-init")
                .InvokeAsync().DefaultTimeout();

            Assert.Equal(CliExitCodes.InvalidCommand, exitCode);
            Assert.Equal(NewCommandStrings.TemplateRepositoryPathInvalid, Assert.Single(interaction.DisplayedErrors));
            Assert.False(Directory.Exists(Path.Combine(workspace.Path, "out")));
        }

        Assert.Empty(Directory.GetFileSystemEntries(Path.Combine(workspace.WorkspaceRoot.FullName, ".aspire", "cache", "templates")));
        Assert.True(Directory.Exists(workspace.Path));
    }

    [Fact]
    public async Task RepositoryTemplate_CloneCancellationDisposesProcessAndCheckout()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        using var cancellation = new CancellationTokenSource();
        var interaction = new TestInteractionService();
        var processFactory = new TestProcessExecutionFactory
        {
            AsyncAttemptCallback = (_, _, token) =>
            {
                cancellation.Cancel();
                return Task.FromCanceled<(int ExitCode, string? Stdout)>(token);
            }
        };
        var services = CreateServices(workspace, interaction, interactive: false);
        services.AddSingleton<IProcessExecutionFactory>(processFactory);
        using (var provider = services.BuildServiceProvider())
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                provider.GetRequiredService<ITemplateProvider>().GetTemplatesAsync(
                    new TemplateSource("template", "https://example.test/templates.git"), cancellation.Token));

            Assert.Equal(1, Assert.IsType<TestProcessExecution>(Assert.Single(processFactory.CreatedExecutions)).DisposeCount);
            Assert.False(Directory.Exists(Path.Combine(workspace.Path, "out")));
        }

        Assert.Empty(Directory.GetFileSystemEntries(Path.Combine(workspace.WorkspaceRoot.FullName, ".aspire", "cache", "templates")));
    }

    [Fact]
    public async Task RepositoryTemplate_UsesUniqueCheckoutsAndLiteralGitArguments()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var interaction = new TestInteractionService();
        List<string> checkouts = [];
        var processFactory = new TestProcessExecutionFactory
        {
            FileNameAssertionCallback = (fileName, arguments, environment, workingDirectory, _) =>
            {
                Assert.Equal("git", fileName);
                Assert.Equal(["clone", "--depth", "1", "--no-hardlinks", "--", "-repository", arguments[^1]], arguments);
                Assert.NotNull(environment);
                Assert.Equal("0", environment["GIT_TERMINAL_PROMPT"]);
                Assert.Equal(workspace.WorkspaceRoot.FullName, workingDirectory.FullName);
                checkouts.Add(arguments[^1]);
            }
        };
        processFactory.AsyncAttemptCallback = async (_, _, token) =>
        {
            Directory.CreateDirectory(Path.Combine(checkouts[^1], ".git"));
            var readOnlyFile = Path.Combine(checkouts[^1], ".git", "read-only-object");
            await File.WriteAllTextAsync(readOnlyFile, "object content", token);
            File.SetAttributes(readOnlyFile, FileAttributes.ReadOnly);
            await CreateTemplateAsync(workspace, Path.GetRelativePath(workspace.Path, Path.Combine(checkouts[^1], "template")));
            return (0, null);
        };
        var services = CreateServices(workspace, interaction, interactive: false);
        services.AddSingleton<IProcessExecutionFactory>(processFactory);
        using (var provider = services.BuildServiceProvider())
        {
            var templates = provider.GetRequiredService<ITemplateProvider>();
            var first = Assert.Single(await templates.GetTemplatesAsync(new TemplateSource("template", "-repository")));
            Assert.Single(await templates.GetTemplatesAsync(new TemplateSource("template", "-repository")));
            Assert.Equal(2, checkouts.Distinct().Count());
            var cacheRoot = Path.Combine(provider.GetRequiredService<CliExecutionContext>().CacheDirectory.FullName, "templates");
            Assert.All(checkouts, path => Assert.Equal(cacheRoot, Directory.GetParent(path)!.Parent!.FullName));

            var command = provider.GetRequiredService<RootCommand>();
            var parseResult = command.Parse("new --template-repository=-repository --template-path template --name Demo --output out --suppress-agent-init");
            var result = await first.ApplyTemplateAsync(new TemplateInputs { Name = "Demo", Output = "out" }, parseResult, TestContext.Current.CancellationToken);
            Assert.Equal(CliExitCodes.Success, result.ExitCode);
            await Verify(ReadOutput(workspace)).ScrubInlineGuids();
        }

        Assert.All(checkouts, path =>
        {
            Assert.False(Directory.Exists(path));
            Assert.False(File.Exists(TemporaryCacheDirectory.GetLeasePath(Directory.GetParent(path)!.FullName)));
        });
    }

    [Theory]
    [InlineData("--template-repository \"\"")]
    [InlineData("--template-repository https://example.test/templates.git --template-path \"\"")]
    public async Task RepositoryTemplate_RejectsEmptySourceOptions(string options)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var interaction = new TestInteractionService();
        var processFactory = new TestProcessExecutionFactory();
        var services = CreateServices(workspace, interaction, interactive: false);
        services.AddSingleton<IProcessExecutionFactory>(processFactory);
        using var provider = services.BuildServiceProvider();

        var exitCode = await provider.GetRequiredService<RootCommand>()
            .Parse($"new {options} --name Demo --output out --suppress-agent-init").InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.InvalidCommand, exitCode);
        Assert.Equal(NewCommandStrings.TemplatePathConflictingOptions, Assert.Single(interaction.DisplayedErrors));
        Assert.Empty(processFactory.CreatedExecutions);
    }

    [Theory]
    [InlineData("--source ./packages")]
    [InlineData("--version 1.0.0")]
    [InlineData("--channel daily")]
    [InlineData("--language csharp")]
    [InlineData("aspire-starter")]
    public async Task RepositoryTemplate_RejectsConflictingOptions(string options)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var interaction = new TestInteractionService();
        using var provider = CreateServices(workspace, interaction, interactive: false).BuildServiceProvider();

        var exitCode = await provider.GetRequiredService<RootCommand>()
            .Parse($"new {options} --template-repository https://example.test/templates.git --suppress-agent-init").InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.InvalidCommand, exitCode);
        Assert.Equal(NewCommandStrings.TemplatePathConflictingOptions, Assert.Single(interaction.DisplayedErrors));
        Assert.False(Directory.Exists(Path.Combine(workspace.WorkspaceRoot.FullName, ".aspire", "cache", "templates")));
    }

    [Fact]
    public async Task RepositoryTemplate_HelpDoesNotCloneRepository()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var interaction = new TestInteractionService();
        var processFactory = new TestProcessExecutionFactory();
        var services = CreateServices(workspace, interaction, interactive: false);
        services.AddSingleton<IProcessExecutionFactory>(processFactory);
        using var provider = services.BuildServiceProvider();

        var exitCode = await provider.GetRequiredService<RootCommand>()
            .Parse("new --template-repository https://example.test/templates.git --help").InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, exitCode);
        Assert.Empty(processFactory.CreatedExecutions);
        Assert.False(Directory.Exists(Path.Combine(workspace.WorkspaceRoot.FullName, ".aspire", "cache", "templates")));
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
