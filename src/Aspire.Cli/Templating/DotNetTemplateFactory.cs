// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Certificates;
using Aspire.Cli.Commands;
using Aspire.Cli.Interaction;
using Aspire.Cli.NuGet;
using Aspire.Cli.Utils;
using Semver;

namespace Aspire.Cli.Templating;

internal class DotNetTemplateFactory(IInteractionService interactionService, IDotNetCliRunner runner, ICertificateService certificateService, INuGetPackageCache nuGetPackageCache, INewCommandPrompter prompter) : ITemplateFactory
{
    public IEnumerable<ITemplate> GetTemplates()
    {
        yield return new CallbackTemplate(
            "aspire-starter",
            "Aspire Starter App",
            projectName => $"./{projectName}",
            ApplyExtraAspireStarterOptions,
            (template, parseResult, ct) => ApplyTemplateAsync(template, parseResult, PromptForExtraAspireStarterOptionsAsync, ct)
            );
            
        yield return new CallbackTemplate(
            "aspire",
            "Aspire Empty App",
            projectName => $"./{projectName}",
            _ => { },
            ApplyTemplateWithNoExtraArgsAsync
            );
            
        yield return new CallbackTemplate(
            "aspire-apphost",
            "Aspire App Host",
            projectName => $"./{projectName}",
            _ => { },
            ApplyTemplateWithNoExtraArgsAsync
            );
            
        yield return new CallbackTemplate(
            "aspire-servicedefaults",
            "Aspire Service Defaults",
            projectName => $"./{projectName}",
            _ => { },
            ApplyTemplateWithNoExtraArgsAsync
            );
            
        yield return new CallbackTemplate(
            "aspire-mstest",
            "Aspire Test Project (MSTest)",
            projectName => $"./{projectName}",
            _ => { },
            ApplyTemplateWithNoExtraArgsAsync
            );
            
        yield return new CallbackTemplate(
            "aspire-nunit",
            "Aspire Test Project (NUnit)",
            projectName => $"./{projectName}",
            _ => { },
            ApplyTemplateWithNoExtraArgsAsync
            );
            
        yield return new CallbackTemplate(
            "aspire-xunit",
            "Aspire Test Project (xUnit)",
            projectName => $"./{projectName}",
            _ => { },
            (template, parseResult, ct) => ApplyTemplateAsync(template, parseResult, PromptForExtraAspireXUnitOptionsAsync, ct)
            );
    }

    private async Task<string[]> PromptForExtraAspireStarterOptionsAsync(ParseResult result, CancellationToken cancellationToken)
    {
        var extraArgs = new List<string>();

        await PromptForRedisCacheOptionAsync(result, extraArgs, cancellationToken);
        await PromptForTestFrameworkOptionsAsync(result, extraArgs, cancellationToken);

        return extraArgs.ToArray();
    }

    private async Task<string[]> PromptForExtraAspireXUnitOptionsAsync(ParseResult result, CancellationToken cancellationToken)
    {
        var extraArgs = new List<string>();

        await PromptForXUnitVersionOptionsAsync(result, extraArgs, cancellationToken);

        return extraArgs.ToArray();
    }

    private async Task PromptForRedisCacheOptionAsync(ParseResult result, List<string> extraArgs, CancellationToken cancellationToken)
    {
        var useRedisCache = result.GetValue<bool?>("--use-redis-cache");
        if (!useRedisCache.HasValue)
        {
            useRedisCache = await interactionService.PromptForSelectionAsync("Use Redis Cache", ["Yes", "No"], choice => choice, cancellationToken) switch
            {
                "Yes" => true,
                "No" => false,
                _ => throw new InvalidOperationException("Unexpected choice for Redis Cache option.")
            };
        }

        if (useRedisCache ?? false)
        {
            interactionService.DisplayMessage("french_fries", "Using Redis Cache for caching.");
            extraArgs.Add("--use-redis-cache");
        }
    }

    private async Task PromptForTestFrameworkOptionsAsync(ParseResult result, List<string> extraArgs, CancellationToken cancellationToken)
    {
        var testFramework = result.GetValue<string?>("--test-framework");

        if (testFramework is null)
        {
            var createTestProject = await interactionService.PromptForSelectionAsync(
                "Do you want to create a test project?",
                ["Yes", "No"],
                choice => choice,
                cancellationToken);

            if (createTestProject == "No")
            {
                return;
            }
        }

        if (string.IsNullOrEmpty(testFramework))
        {
            testFramework = await interactionService.PromptForSelectionAsync(
                "Select a test framework",
                ["MSTest", "NUnit", "xUnit.net", "None"],
                choice => choice,
                cancellationToken);
        }

        if (testFramework is { } && testFramework != "None")
        {
            if (testFramework.ToLower() == "xunit.net")
            {
                await PromptForXUnitVersionOptionsAsync(result, extraArgs, cancellationToken);
            }

            interactionService.DisplayMessage("french_fries", $"Using {testFramework} for testing.");

            extraArgs.Add("--test-framework");
            extraArgs.Add(testFramework);
        }
    }

    private async Task PromptForXUnitVersionOptionsAsync(ParseResult result, List<string> extraArgs, CancellationToken cancellationToken)
    {
        var xunitVersion = result.GetValue<string?>("--xunit-version");
        if (string.IsNullOrEmpty(xunitVersion))
        {
            xunitVersion = await interactionService.PromptForSelectionAsync(
                "Enter the xUnit.net version to use",
                ["v2", "v3", "v3mtp"],
                choice => choice,
                cancellationToken: cancellationToken);
        }

        extraArgs.Add("--xunit-version");
        extraArgs.Add(xunitVersion);
    }

    private static void ApplyExtraAspireStarterOptions(Command command)
    {
        var useRedisCacheOption = new Option<bool?>("--use-redis-cache");
        useRedisCacheOption.Description = "Configures whether to setup the application to use Redis for caching.";
        useRedisCacheOption.DefaultValueFactory = _ => false;
        command.Options.Add(useRedisCacheOption);

        var testFrameworkOption = new Option<string?>("--test-framework");
        testFrameworkOption.Description = "Configures whether to create a project for integration tests using MSTest, NUnit, or xUnit.net.";
        command.Options.Add(testFrameworkOption);

        var xunitVersionOption = new Option<string?>("--xunit-version");
        xunitVersionOption.Description = "The version of xUnit.net to use for the test project.";
        command.Options.Add(xunitVersionOption);
    }

    private async Task<int> ApplyTemplateWithNoExtraArgsAsync(CallbackTemplate template, ParseResult parseResult, CancellationToken cancellationToken)
    {
        return await ApplyTemplateAsync(template, parseResult, (_, _) => Task.FromResult(Array.Empty<string>()), cancellationToken);
    }

    private async Task<int> ApplyTemplateAsync(CallbackTemplate template, ParseResult parseResult, Func<ParseResult, CancellationToken, Task<string[]>> extraArgsCallback, CancellationToken cancellationToken)
    {
        try
        {
            var name = await GetProjectNameAsync(parseResult, cancellationToken);
            var outputPath = await GetOutputPathAsync(parseResult, template.PathDeriver, name, cancellationToken);

            // Some templates have additional arguments that need to be applied to the `dotnet new` command
            // when it is executed. This callback will get those arguments and potentially prompt for them.
            var extraArgs = await extraArgsCallback(parseResult, cancellationToken);

            var source = parseResult.GetValue<string?>("--source");
            var version = await GetProjectTemplatesVersionAsync(parseResult, prerelease: true, source: source, cancellationToken: cancellationToken);

            var templateInstallCollector = new OutputCollector();
            var templateInstallResult = await interactionService.ShowStatusAsync<(int ExitCode, string? TemplateVersion)>(
                ":ice:  Getting latest templates...",
                async () =>
                {
                    var options = new DotNetCliRunnerInvocationOptions()
                    {
                        StandardOutputCallback = templateInstallCollector.AppendOutput,
                        StandardErrorCallback = templateInstallCollector.AppendOutput,
                    };

                    var result = await runner.InstallTemplateAsync("Aspire.ProjectTemplates", version, source, true, options, cancellationToken);
                    return result;
                });

            if (templateInstallResult.ExitCode != 0)
            {
                interactionService.DisplayLines(templateInstallCollector.GetLines());
                interactionService.DisplayError($"The template installation failed with exit code {templateInstallResult.ExitCode}. For more information run with --debug switch.");
                return ExitCodeConstants.FailedToInstallTemplates;
            }

            interactionService.DisplayMessage($"package", $"Using project templates version: {templateInstallResult.TemplateVersion}");

            var newProjectCollector = new OutputCollector();
            var newProjectExitCode = await interactionService.ShowStatusAsync(
                ":rocket:  Creating new Aspire project...",
                async () =>
                {
                    var options = new DotNetCliRunnerInvocationOptions()
                    {
                        StandardOutputCallback = newProjectCollector.AppendOutput,
                        StandardErrorCallback = newProjectCollector.AppendOutput,
                    };
                    
                    var result = await runner.NewProjectAsync(
                                template.Name,
                                name,
                                outputPath,
                                extraArgs,
                                options,
                                cancellationToken);

                    return result;
                });

            if (newProjectExitCode != 0)
            {
                interactionService.DisplayLines(newProjectCollector.GetLines());
                interactionService.DisplayError($"Project creation failed with exit code {newProjectExitCode}. For more information run with --debug switch.");
                return ExitCodeConstants.FailedToCreateNewProject;
            }

            await certificateService.EnsureCertificatesTrustedAsync(runner, cancellationToken);

            interactionService.DisplaySuccess($"Project created successfully in {outputPath}.");

            return ExitCodeConstants.Success;
        }
        catch (OperationCanceledException)
        {
            interactionService.DisplayCancellationMessage();
            return ExitCodeConstants.FailedToCreateNewProject;
        }
        catch (CertificateServiceException ex)
        {
            interactionService.DisplayError($"An error occurred while trusting the certificates: {ex.Message}");
            return ExitCodeConstants.FailedToTrustCertificates;
        }
        catch (EmptyChoicesException ex)
        {
            interactionService.DisplayError(ex.Message);
            return ExitCodeConstants.FailedToCreateNewProject;
        }
    }
     
    private async Task<string> GetProjectNameAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (parseResult.GetValue<string>("--name") is not { } name || !ProjectNameValidator.IsProjectNameValid(name))
        {
            var defaultName = new DirectoryInfo(Environment.CurrentDirectory).Name;
            name = await prompter.PromptForProjectNameAsync(defaultName, cancellationToken);
        }

        return name;
    }

    private async Task<string> GetOutputPathAsync(ParseResult parseResult, Func<string, string> pathDeriver, string projectName, CancellationToken cancellationToken)
    {
        if (parseResult.GetValue<string>("--output") is not { } outputPath)
        {
            outputPath = await prompter.PromptForOutputPath(pathDeriver(projectName), cancellationToken);
        }

        return Path.GetFullPath(outputPath);
    }

    private async Task<string> GetProjectTemplatesVersionAsync(ParseResult parseResult, bool prerelease, string? source, CancellationToken cancellationToken)
    {
        if (parseResult.GetValue<string>("--version") is { } version)
        {
            return version;
        }
        else
        {
            var workingDirectory = new DirectoryInfo(Environment.CurrentDirectory);

            var candidatePackages = await interactionService.ShowStatusAsync(
                "Searching for available project template versions...",
                () => nuGetPackageCache.GetTemplatePackagesAsync(workingDirectory, prerelease, source, cancellationToken)
                );

            if (!candidatePackages.Any())
            {
                throw new EmptyChoicesException("No template versions were found. Please check your internet connection or NuGet source configuration.");
            }

            var orderedCandidatePackages = candidatePackages.OrderByDescending(p => SemVersion.Parse(p.Version), SemVersion.PrecedenceComparer);
            var selectedPackage = await prompter.PromptForTemplatesVersionAsync(orderedCandidatePackages, cancellationToken);
            return selectedPackage.Version;
        }
    }
}

