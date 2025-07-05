// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using Aspire.Cli.Certificates;
using Aspire.Cli.Commands;
using Aspire.Cli.Interaction;
using Aspire.Cli.NuGet;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Semver;

namespace Aspire.Cli.Templating;

internal class DotNetTemplateFactory(IInteractionService interactionService, IDotNetCliRunner runner, ICertificateService certificateService, INuGetPackageCache nuGetPackageCache, INewCommandPrompter prompter) : ITemplateFactory
{
    public IEnumerable<ITemplate> GetTemplates()
    {
        yield return new CallbackTemplate(
            "aspire-starter",
            TemplatingStrings.AspireStarter_Description,
            projectName => $"./{projectName}",
            ApplyExtraAspireStarterOptions,
            (template, parseResult, ct) => ApplyTemplateAsync(template, parseResult, PromptForExtraAspireStarterOptionsAsync, ct)
            );

        yield return new CallbackTemplate(
            "aspire",
            TemplatingStrings.AspireEmpty_Description,
            projectName => $"./{projectName}",
            _ => { },
            ApplyTemplateWithNoExtraArgsAsync
            );

        yield return new CallbackTemplate(
            "aspire-apphost",
            TemplatingStrings.AspireAppHost_Description,
            projectName => $"./{projectName}",
            _ => { },
            ApplyTemplateWithNoExtraArgsAsync
            );

        yield return new CallbackTemplate(
            "aspire-servicedefaults",
            TemplatingStrings.AspireServiceDefaults_Description,
            projectName => $"./{projectName}",
            _ => { },
            ApplyTemplateWithNoExtraArgsAsync
            );

        yield return new CallbackTemplate(
            "aspire-mstest",
            TemplatingStrings.AspireMSTest_Description,
            projectName => $"./{projectName}",
            _ => { },
            ApplyTemplateWithNoExtraArgsAsync
            );

        yield return new CallbackTemplate(
            "aspire-nunit",
            TemplatingStrings.AspireNUnit_Description,
            projectName => $"./{projectName}",
            _ => { },
            ApplyTemplateWithNoExtraArgsAsync
            );

        yield return new CallbackTemplate(
            "aspire-xunit",
            TemplatingStrings.AspireXUnit_Description,
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
            useRedisCache = await interactionService.PromptForSelectionAsync(TemplatingStrings.UseRedisCache_Prompt, [TemplatingStrings.Yes, TemplatingStrings.No], choice => choice, cancellationToken) switch
            {
                var choice when string.Equals(choice, TemplatingStrings.Yes, StringComparisons.CliInputOrOutput) => true,
                var choice when string.Equals(choice, TemplatingStrings.No, StringComparisons.CliInputOrOutput) => false,
                _ => throw new InvalidOperationException(TemplatingStrings.UseRedisCache_UnexpectedChoice)
            };
        }

        if (useRedisCache ?? false)
        {
            interactionService.DisplayMessage("french_fries", TemplatingStrings.UseRedisCache_UsingRedisCache);
            extraArgs.Add("--use-redis-cache");
        }
    }

    private async Task PromptForTestFrameworkOptionsAsync(ParseResult result, List<string> extraArgs, CancellationToken cancellationToken)
    {
        var testFramework = result.GetValue<string?>("--test-framework");

        if (testFramework is null)
        {
            var createTestProject = await interactionService.PromptForSelectionAsync(
                TemplatingStrings.PromptForTFMOptions_Prompt,
                [TemplatingStrings.No, TemplatingStrings.Yes],
                choice => choice,
                cancellationToken);

            if (string.Equals(createTestProject, TemplatingStrings.No, StringComparisons.CliInputOrOutput))
            {
                return;
            }
        }

        if (string.IsNullOrEmpty(testFramework))
        {
            testFramework = await interactionService.PromptForSelectionAsync(
                TemplatingStrings.PromptForTFM_Prompt,
                ["MSTest", "NUnit", "xUnit.net", TemplatingStrings.None],
                choice => choice,
                cancellationToken);
        }

        if (testFramework is { } && !string.Equals(testFramework, TemplatingStrings.None, StringComparisons.CliInputOrOutput))
        {
            if (testFramework.ToLower() == "xunit.net")
            {
                await PromptForXUnitVersionOptionsAsync(result, extraArgs, cancellationToken);
            }

            interactionService.DisplayMessage("french_fries", string.Format(CultureInfo.CurrentCulture, TemplatingStrings.PromptForTFM_UsingForTesting, testFramework));

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
                TemplatingStrings.EnterXUnitVersion_Prompt,
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
        useRedisCacheOption.Description = TemplatingStrings.UseRedisCache_Description;
        useRedisCacheOption.DefaultValueFactory = _ => false;
        command.Options.Add(useRedisCacheOption);

        var testFrameworkOption = new Option<string?>("--test-framework");
        testFrameworkOption.Description = TemplatingStrings.PromptForTFMOptions_Description;
        command.Options.Add(testFrameworkOption);

        var xunitVersionOption = new Option<string?>("--xunit-version");
        xunitVersionOption.Description = TemplatingStrings.EnterXUnitVersion_Description;
        command.Options.Add(xunitVersionOption);
    }

    private async Task<TemplateResult> ApplyTemplateWithNoExtraArgsAsync(CallbackTemplate template, ParseResult parseResult, CancellationToken cancellationToken)
    {
        return await ApplyTemplateAsync(template, parseResult, (_, _) => Task.FromResult(Array.Empty<string>()), cancellationToken);
    }

    private async Task<TemplateResult> ApplyTemplateAsync(CallbackTemplate template, ParseResult parseResult, Func<ParseResult, CancellationToken, Task<string[]>> extraArgsCallback, CancellationToken cancellationToken)
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
                $":ice:  {TemplatingStrings.GettingLatestTemplates}",
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
                interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, TemplatingStrings.TemplateInstallationFailed, templateInstallResult.ExitCode));
                return new TemplateResult(ExitCodeConstants.FailedToInstallTemplates);
            }

            interactionService.DisplayMessage($"package", string.Format(CultureInfo.CurrentCulture, TemplatingStrings.UsingProjectTemplatesVersion, templateInstallResult.TemplateVersion));

            var newProjectCollector = new OutputCollector();
            var newProjectExitCode = await interactionService.ShowStatusAsync(
                $":rocket:  {TemplatingStrings.CreatingNewProject}",
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
                interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, TemplatingStrings.ProjectCreationFailed, newProjectExitCode));
                return new TemplateResult(ExitCodeConstants.FailedToCreateNewProject);
            }

            await certificateService.EnsureCertificatesTrustedAsync(runner, cancellationToken);

            interactionService.DisplaySuccess(string.Format(CultureInfo.CurrentCulture, TemplatingStrings.ProjectCreatedSuccessfully, outputPath));

            return new TemplateResult(ExitCodeConstants.Success, outputPath);
        }
        catch (OperationCanceledException)
        {
            interactionService.DisplayCancellationMessage();
            return new TemplateResult(ExitCodeConstants.FailedToCreateNewProject);
        }
        catch (CertificateServiceException ex)
        {
            interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, TemplatingStrings.CertificateTrustError, ex.Message));
            return new TemplateResult(ExitCodeConstants.FailedToTrustCertificates);
        }
        catch (EmptyChoicesException ex)
        {
            interactionService.DisplayError(ex.Message);
            return new TemplateResult(ExitCodeConstants.FailedToCreateNewProject);
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
                TemplatingStrings.SearchingForAvailableTemplateVersions,
                () => nuGetPackageCache.GetTemplatePackagesAsync(workingDirectory, prerelease, source, cancellationToken)
                );

            if (!candidatePackages.Any())
            {
                throw new EmptyChoicesException(TemplatingStrings.NoTemplateVersionsFound);
            }

            var orderedCandidatePackages = candidatePackages.OrderByDescending(p => SemVersion.Parse(p.Version), SemVersion.PrecedenceComparer);
            var selectedPackage = await prompter.PromptForTemplatesVersionAsync(orderedCandidatePackages, cancellationToken);
            return selectedPackage.Version;
        }
    }
}

