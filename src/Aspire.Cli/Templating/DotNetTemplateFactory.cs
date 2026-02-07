// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Cli.Certificates;
using Aspire.Cli.Commands;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.Packaging;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using NuGetPackage = Aspire.Shared.NuGetPackageCli;
using Semver;
using Spectre.Console;

namespace Aspire.Cli.Templating;

internal class DotNetTemplateFactory(
    IInteractionService interactionService,
    IDotNetCliRunner runner,
    ICertificateService certificateService,
    IPackagingService packagingService,
    INewCommandPrompter prompter,
    CliExecutionContext executionContext,
    IFeatures features,
    IConfigurationService configurationService)
    : ITemplateFactory
{
    // Template-specific options
    private static readonly Option<bool?> s_localhostTldOption = new("--localhost-tld")
    {
        Description = TemplatingStrings.UseLocalhostTld_Description,
        DefaultValueFactory = _ => false
    };
    private static readonly Option<bool?> s_useRedisCacheOption = new("--use-redis-cache")
    {
        Description = TemplatingStrings.UseRedisCache_Description,
        DefaultValueFactory = _ => false
    };
    private static readonly Option<string?> s_testFrameworkOption = new("--test-framework")
    {
        Description = TemplatingStrings.PromptForTFMOptions_Description
    };
    private static readonly Option<string?> s_xunitVersionOption = new("--xunit-version")
    {
        Description = TemplatingStrings.EnterXUnitVersion_Description
    };

    public IEnumerable<ITemplate> GetTemplates()
    {
        var showAllTemplates = features.IsFeatureEnabled(KnownFeatures.ShowAllTemplates, false);
        return GetTemplatesCore(showAllTemplates);
    }

    public IEnumerable<ITemplate> GetInitTemplates()
    {
        return GetTemplatesCore(showAllTemplates: true, nonInteractive: true);
    }

    private IEnumerable<ITemplate> GetTemplatesCore(bool showAllTemplates, bool nonInteractive = false)
    {
        yield return new CallbackTemplate(
            "aspire-starter",
            TemplatingStrings.AspireStarter_Description,
            projectName => $"./{projectName}",
            ApplyExtraAspireStarterOptions,
            nonInteractive
                ? ApplyTemplateWithNoExtraArgsAsync
                : (template, inputs, parseResult, ct) => ApplyTemplateAsync(template, inputs, parseResult, PromptForExtraAspireStarterOptionsAsync, ct)
            );

        yield return new CallbackTemplate(
            "aspire-ts-cs-starter",
            TemplatingStrings.AspireJsFrontendStarter_Description,
            projectName => $"./{projectName}",
            ApplyExtraAspireJsFrontendStarterOptions,
            nonInteractive
                ? ApplyTemplateWithNoExtraArgsAsync
                : (template, inputs, parseResult, ct) => ApplyTemplateAsync(template, inputs, parseResult, PromptForExtraAspireJsFrontendStarterOptionsAsync, ct)
            );

        yield return new CallbackTemplate(
            "aspire-py-starter",
            TemplatingStrings.AspirePyStarter_Description,
            projectName => $"./{projectName}",
            ApplyDevLocalhostTldOption,
            nonInteractive
                ? ApplySingleFileTemplateWithNoExtraArgsAsync
                : (template, inputs, parseResult, ct) => ApplySingleFileTemplate(template, inputs, parseResult, PromptForExtraAspirePythonStarterOptionsAsync, ct)
            );

        // Single-file AppHost templates
        yield return new CallbackTemplate(
            "aspire-apphost-singlefile",
            TemplatingStrings.AspireAppHostSingleFile_Description,
            projectName => $"./{projectName}",
            ApplyDevLocalhostTldOption,
            nonInteractive
                ? ApplySingleFileTemplateWithNoExtraArgsAsync
                : (template, inputs, parseResult, ct) => ApplySingleFileTemplate(template, inputs, parseResult, PromptForExtraAspireSingleFileOptionsAsync, ct)
            );

        if (showAllTemplates)
        {
            yield return new CallbackTemplate(
                "aspire",
                TemplatingStrings.AspireEmpty_Description,
                projectName => $"./{projectName}",
                ApplyDevLocalhostTldOption,
                ApplyTemplateWithNoExtraArgsAsync
                );

            yield return new CallbackTemplate(
                "aspire-apphost",
                TemplatingStrings.AspireAppHost_Description,
                projectName => $"./{projectName}",
                ApplyDevLocalhostTldOption,
                ApplyTemplateWithNoExtraArgsAsync
                );

            yield return new CallbackTemplate(
                "aspire-servicedefaults",
                TemplatingStrings.AspireServiceDefaults_Description,
                projectName => $"./{projectName}",
                _ => { },
                ApplyTemplateWithNoExtraArgsAsync
                );
        }

        // Folded into the last yieled template.
        var msTestTemplate = new CallbackTemplate(
            "aspire-mstest",
            TemplatingStrings.AspireMSTest_Description,
            projectName => $"./{projectName}",
            _ => { },
            ApplyTemplateWithNoExtraArgsAsync
            );

        // Folded into the last yielded template.
        var nunitTemplate = new CallbackTemplate(
            "aspire-nunit",
            TemplatingStrings.AspireNUnit_Description,
            projectName => $"./{projectName}",
            _ => { },
            ApplyTemplateWithNoExtraArgsAsync
            );

        // Folded into the last yielded template.
        var xunitTemplate = new CallbackTemplate(
            "aspire-xunit",
            TemplatingStrings.AspireXUnit_Description,
            projectName => $"./{projectName}",
            _ => { },
            nonInteractive
                ? ApplyTemplateWithNoExtraArgsAsync
                : (template, inputs, parseResult, ct) => ApplyTemplateAsync(template, inputs, parseResult, PromptForExtraAspireXUnitOptionsAsync, ct)
            );

        // Prepends a test framework selection step then calls the
        // underlying test template.
        if (showAllTemplates)
        {
            yield return new CallbackTemplate(
                "aspire-test",
                TemplatingStrings.IntegrationTestsTemplate_Description,
                projectName => $"./{projectName}",
                _ => { },
                async (template, inputs, parseResult, ct) =>
                {
                    var testTemplate = await prompter.PromptForTemplateAsync(
                        [msTestTemplate, xunitTemplate, nunitTemplate],
                        ct
                    );

                    var testCallbackTemplate = (CallbackTemplate)testTemplate;
                    return await testCallbackTemplate.ApplyTemplateAsync(inputs, parseResult, ct);
                });
        }
    }

    private async Task<string[]> PromptForExtraAspireStarterOptionsAsync(ParseResult result, CancellationToken cancellationToken)
    {
        var extraArgs = new List<string>();

        await PromptForDevLocalhostTldOptionAsync(result, extraArgs, cancellationToken);
        await PromptForRedisCacheOptionAsync(result, extraArgs, cancellationToken);
        await PromptForTestFrameworkOptionsAsync(result, extraArgs, cancellationToken);

        return extraArgs.ToArray();
    }

    private async Task<string[]> PromptForExtraAspireSingleFileOptionsAsync(ParseResult result, CancellationToken cancellationToken)
    {
        var extraArgs = new List<string>();

        await PromptForDevLocalhostTldOptionAsync(result, extraArgs, cancellationToken);

        return extraArgs.ToArray();
    }

    private async Task<string[]> PromptForExtraAspirePythonStarterOptionsAsync(ParseResult result, CancellationToken cancellationToken)
    {
        var extraArgs = new List<string>();

        await PromptForDevLocalhostTldOptionAsync(result, extraArgs, cancellationToken);
        await PromptForRedisCacheOptionAsync(result, extraArgs, cancellationToken);

        return extraArgs.ToArray();
    }

    private async Task<string[]> PromptForExtraAspireJsFrontendStarterOptionsAsync(ParseResult result, CancellationToken cancellationToken)
    {
        var extraArgs = new List<string>();

        await PromptForDevLocalhostTldOptionAsync(result, extraArgs, cancellationToken);
        await PromptForRedisCacheOptionAsync(result, extraArgs, cancellationToken);

        return extraArgs.ToArray();
    }

    private async Task<string[]> PromptForExtraAspireXUnitOptionsAsync(ParseResult result, CancellationToken cancellationToken)
    {
        var extraArgs = new List<string>();

        await PromptForXUnitVersionOptionsAsync(result, extraArgs, cancellationToken);

        return extraArgs.ToArray();
    }

    private async Task PromptForDevLocalhostTldOptionAsync(ParseResult result, List<string> extraArgs, CancellationToken cancellationToken)
    {
        var useLocalhostTld = result.GetValue(s_localhostTldOption);
        if (!useLocalhostTld.HasValue)
        {
            useLocalhostTld = await interactionService.PromptForSelectionAsync(TemplatingStrings.UseLocalhostTld_Prompt, [TemplatingStrings.No, TemplatingStrings.Yes], choice => choice, cancellationToken) switch
            {
                var choice when string.Equals(choice, TemplatingStrings.Yes, StringComparisons.CliInputOrOutput) => true,
                var choice when string.Equals(choice, TemplatingStrings.No, StringComparisons.CliInputOrOutput) => false,
                _ => throw new InvalidOperationException(TemplatingStrings.UseLocalhostTld_UnexpectedChoice)
            };
        }

        if (useLocalhostTld ?? false)
        {
            interactionService.DisplayMessage("check_mark", TemplatingStrings.UseLocalhostTld_UsingLocalhostTld);
            extraArgs.Add("--localhost-tld");
        }
    }

    private async Task PromptForRedisCacheOptionAsync(ParseResult result, List<string> extraArgs, CancellationToken cancellationToken)
    {
        var useRedisCache = result.GetValue(s_useRedisCacheOption);
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
            interactionService.DisplayMessage("check_mark", TemplatingStrings.UseRedisCache_UsingRedisCache);
            extraArgs.Add("--use-redis-cache");
        }
    }

    private async Task PromptForTestFrameworkOptionsAsync(ParseResult result, List<string> extraArgs, CancellationToken cancellationToken)
    {
        var testFramework = result.GetValue(s_testFrameworkOption);

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

            interactionService.DisplayMessage("check_mark", string.Format(CultureInfo.CurrentCulture, TemplatingStrings.PromptForTFM_UsingForTesting, testFramework));

            extraArgs.Add("--test-framework");
            extraArgs.Add(testFramework);
        }
    }

    private async Task PromptForXUnitVersionOptionsAsync(ParseResult result, List<string> extraArgs, CancellationToken cancellationToken)
    {
        var xunitVersion = result.GetValue(s_xunitVersionOption);
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
        ApplyDevLocalhostTldOption(command);

        command.Options.Add(s_useRedisCacheOption);
        command.Options.Add(s_testFrameworkOption);
        command.Options.Add(s_xunitVersionOption);
    }

    private static void ApplyExtraAspireJsFrontendStarterOptions(Command command)
    {
        ApplyDevLocalhostTldOption(command);

        command.Options.Add(s_useRedisCacheOption);
    }

    private static void ApplyDevLocalhostTldOption(Command command)
    {
        command.Options.Add(s_localhostTldOption);
    }

    private async Task<TemplateResult> ApplyTemplateWithNoExtraArgsAsync(CallbackTemplate template, TemplateInputs inputs, ParseResult parseResult, CancellationToken cancellationToken)
    {
        return await ApplyTemplateAsync(template, inputs, parseResult, (_, _) => Task.FromResult(Array.Empty<string>()), cancellationToken);
    }

    private async Task<TemplateResult> ApplySingleFileTemplate(CallbackTemplate template, TemplateInputs inputs, ParseResult parseResult, Func<ParseResult, CancellationToken, Task<string[]>> extraArgsCallback, CancellationToken cancellationToken)
    {
        // For single-file templates invoked via InitCommand, use the working directory as the output
        if (inputs.UseWorkingDirectory)
        {
            return await ApplyTemplateAsync(
                template,
                inputs,
                executionContext.WorkingDirectory.Name,
                executionContext.WorkingDirectory.FullName,
                parseResult,
                extraArgsCallback,
                cancellationToken
                );
        }
        else
        {
            return await ApplyTemplateAsync(
                template,
                inputs,
                parseResult,
                extraArgsCallback,
                cancellationToken
                );
        }
    }

    private Task<TemplateResult> ApplySingleFileTemplateWithNoExtraArgsAsync(CallbackTemplate template, TemplateInputs inputs, ParseResult parseResult, CancellationToken cancellationToken)
    {
        return ApplySingleFileTemplate(
            template,
            inputs,
            parseResult,
            (_, _) => Task.FromResult(Array.Empty<string>()),
            cancellationToken);
    }

    private async Task<TemplateResult> ApplyTemplateAsync(CallbackTemplate template, TemplateInputs inputs, ParseResult parseResult, Func<ParseResult, CancellationToken, Task<string[]>> extraArgsCallback, CancellationToken cancellationToken)
    {
        var name = await GetProjectNameAsync(inputs, cancellationToken);
        var outputPath = await GetOutputPathAsync(inputs, template.PathDeriver, name, cancellationToken);

        return await ApplyTemplateAsync(template, inputs, name, outputPath, parseResult, extraArgsCallback, cancellationToken);
    }

    private async Task<TemplateResult> ApplyTemplateAsync(CallbackTemplate template, TemplateInputs inputs, string name, string outputPath, ParseResult parseResult, Func<ParseResult, CancellationToken, Task<string[]>> extraArgsCallback, CancellationToken cancellationToken)
    {
        try
        {
            var source = inputs.Source;
            var selectedTemplateDetails = await GetProjectTemplatesVersionAsync(inputs, cancellationToken: cancellationToken);

            // Some templates have additional arguments that need to be applied to the `dotnet new` command
            // when it is executed. This callback will get those arguments and potentially prompt for them.
            var extraArgs = await extraArgsCallback(parseResult, cancellationToken);
            using var temporaryConfig = selectedTemplateDetails.Channel.Type == PackageChannelType.Explicit ? await TemporaryNuGetConfig.CreateAsync(selectedTemplateDetails.Channel.Mappings!) : null;

            var templateInstallCollector = new OutputCollector();
            var templateInstallResult = await interactionService.ShowStatusAsync<(int ExitCode, string? TemplateVersion)>(
                $":ice:  {TemplatingStrings.GettingTemplates}",
                async () =>
                {
                    var options = new DotNetCliRunnerInvocationOptions()
                    {
                        StandardOutputCallback = templateInstallCollector.AppendOutput,
                        StandardErrorCallback = templateInstallCollector.AppendOutput,
                    };

                    // Whilst we install the templates - if we are using an explicit channel we need to
                    // generate a temporary NuGet.config file to make sure we install the right package
                    // from the right feed. If we are using an implicit channel then we just use the
                    // ambient configuration (although we should still specify the source) because
                    // the user would have selected it.

                    var result = await runner.InstallTemplateAsync(
                        packageName: "Aspire.ProjectTemplates",
                        version: selectedTemplateDetails.Package.Version,
                        nugetConfigFile: temporaryConfig?.ConfigFile,
                        nugetSource: selectedTemplateDetails.Package.Source,
                        force: true,
                        options: options,
                        cancellationToken: cancellationToken);
                    return result;
                });

            if (templateInstallResult.ExitCode != 0)
            {
                interactionService.DisplayLines(templateInstallCollector.GetLines());
                interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, TemplatingStrings.TemplateInstallationFailed, templateInstallResult.ExitCode, executionContext.LogFilePath));
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
                // Exit code 73 indicates that the output directory already contains files from a previous project
                // See: https://github.com/dotnet/aspire/issues/9685
                if (newProjectExitCode == 73)
                {
                    interactionService.DisplayError(TemplatingStrings.ProjectAlreadyExists);
                    return new TemplateResult(ExitCodeConstants.FailedToCreateNewProject);
                }

                interactionService.DisplayLines(newProjectCollector.GetLines());
                interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, TemplatingStrings.ProjectCreationFailed, newProjectExitCode, executionContext.LogFilePath));
                return new TemplateResult(ExitCodeConstants.FailedToCreateNewProject);
            }

            // Trust certificates (result not used since we're not launching an AppHost)
            _ = await certificateService.EnsureCertificatesTrustedAsync(runner, cancellationToken);

            // For explicit channels, optionally create or update a NuGet.config. If none exists in the current
            // working directory, create one in the newly created project's output directory.
            await PromptToCreateOrUpdateNuGetConfigAsync(selectedTemplateDetails.Channel, outputPath, cancellationToken);

            // Create .aspire/settings.json with appHostPath if an AppHost project is found
            await CreateSettingsFileAsync(outputPath, cancellationToken);
            interactionService.DisplaySuccess(string.Format(CultureInfo.CurrentCulture, TemplatingStrings.ProjectCreatedSuccessfully, outputPath.EscapeMarkup()));

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
        catch (Exceptions.ChannelNotFoundException ex)
        {
            interactionService.DisplayError(ex.Message);
            return new TemplateResult(ExitCodeConstants.FailedToCreateNewProject);
        }
        catch (EmptyChoicesException ex)
        {
            interactionService.DisplayError(ex.Message);
            return new TemplateResult(ExitCodeConstants.FailedToCreateNewProject);
        }
    }

    private async Task<string> GetProjectNameAsync(TemplateInputs inputs, CancellationToken cancellationToken)
    {
        if (inputs.Name is not { } name || !ProjectNameValidator.IsProjectNameValid(name))
        {
            var defaultName = executionContext.WorkingDirectory.Name;
            name = await prompter.PromptForProjectNameAsync(defaultName, cancellationToken);
        }

        return name;
    }

    private async Task<string> GetOutputPathAsync(TemplateInputs inputs, Func<string, string> pathDeriver, string projectName, CancellationToken cancellationToken)
    {
        if (inputs.Output is not { } outputPath)
        {
            outputPath = await prompter.PromptForOutputPath(pathDeriver(projectName), cancellationToken);
        }

        return Path.GetFullPath(outputPath);
    }

    private async Task<(NuGetPackage Package, PackageChannel Channel)> GetProjectTemplatesVersionAsync(TemplateInputs inputs, CancellationToken cancellationToken)
    {
        var allChannels = await packagingService.GetChannelsAsync(cancellationToken);
        
        // Check if channel was provided via inputs (highest priority)
        var channelName = inputs.Channel;
        
        // If no channel in inputs, check for global channel setting
        if (string.IsNullOrEmpty(channelName))
        {
            channelName = await configurationService.GetConfigurationAsync("channel", cancellationToken);
        }
        
        IEnumerable<PackageChannel> channels;
        bool hasChannelSetting = !string.IsNullOrEmpty(channelName);
        
        if (hasChannelSetting)
        {
            // If --channel option is provided or global channel setting exists, find the matching channel
            // (--channel option takes precedence over global setting)
            var matchingChannel = allChannels.FirstOrDefault(c => string.Equals(c.Name, channelName, StringComparison.OrdinalIgnoreCase));
            if (matchingChannel is null)
            {
                throw new Exceptions.ChannelNotFoundException($"No channel found matching '{channelName}'. Valid options are: {string.Join(", ", allChannels.Select(c => c.Name))}");
            }
            channels = new[] { matchingChannel };
        }
        else
        {
            // If there are hives (PR build directories), include all channels.
            // Otherwise, only use the implicit/default channel to avoid prompting.
            var hasHives = executionContext.GetPrHiveCount() > 0;
            channels = hasHives 
                ? allChannels 
                : allChannels.Where(c => c.Type is PackageChannelType.Implicit);
        }

        var packagesFromChannels = await interactionService.ShowStatusAsync(TemplatingStrings.SearchingForAvailableTemplateVersions, async () =>
        {
            var results = new List<(NuGetPackage Package, PackageChannel Channel)>();
            var packagesFromChannelsLock = new object();

            await Parallel.ForEachAsync(channels, cancellationToken, async (channel, ct) =>
            {
                var templatePackages = await channel.GetTemplatePackagesAsync(executionContext.WorkingDirectory, ct);
                lock (packagesFromChannelsLock)
                {
                    results.AddRange(templatePackages.Select(p => (p, channel)));
                }
            });

            return results;
        });

        if (!packagesFromChannels.Any())
        {
            throw new EmptyChoicesException(TemplatingStrings.NoTemplateVersionsFound);
        }

        var orderedPackagesFromChannels = packagesFromChannels.OrderByDescending(p => SemVersion.Parse(p.Package.Version), SemVersion.PrecedenceComparer);

        if (inputs.Version is { } version)
        {
            var explicitPackageFromChannel = orderedPackagesFromChannels.FirstOrDefault(p => p.Package.Version == version);
            if (explicitPackageFromChannel.Package is not null)
            {
                return explicitPackageFromChannel;
            }
        }

        // If channel was specified via --channel option or global setting (but no --version), 
        // automatically select the highest version from that channel without prompting
        if (hasChannelSetting)
        {
            return orderedPackagesFromChannels.First();
        }

        var selectedPackageFromChannel = await prompter.PromptForTemplatesVersionAsync(orderedPackagesFromChannels, cancellationToken);
        return selectedPackageFromChannel;
    }

    /// <summary>
    /// Prompts to create or update a NuGet.config for explicit channels.
    /// When the output directory differs from the working directory, a NuGet.config is created/updated
    /// only in the output directory. When they are the same (in-place creation), existing behavior
    /// is preserved where the working directory NuGet.config is considered for updates.
    /// </summary>
    private async Task PromptToCreateOrUpdateNuGetConfigAsync(PackageChannel channel, string outputPath, CancellationToken cancellationToken)
    {
        if (channel.Type is not PackageChannelType.Explicit)
        {
            return;
        }

        var mappings = channel.Mappings;
        if (mappings is null || mappings.Length == 0)
        {
            return;
        }

        var workingDir = executionContext.WorkingDirectory;
        var outputDir = new DirectoryInfo(outputPath);

        // Determine if we're creating the project in-place (output directory same as working directory)
        var normalizedOutputPath = Path.GetFullPath(outputPath);
        var normalizedWorkingPath = workingDir.FullName;
        var isInPlaceCreation = string.Equals(normalizedOutputPath, normalizedWorkingPath, StringComparison.OrdinalIgnoreCase);

        var nugetConfigPrompter = new NuGetConfigPrompter(interactionService);

        if (!isInPlaceCreation)
        {
            // For subdirectory creation, always create/update NuGet.config in the output directory only
            // and ignore any existing NuGet.config in the working directory
            await nugetConfigPrompter.CreateOrUpdateWithoutPromptAsync(outputDir, channel, cancellationToken);
            return;
        }

        // In-place creation: preserve existing behavior
        // Prompt user before creating or updating NuGet.config
        await nugetConfigPrompter.PromptToCreateOrUpdateAsync(workingDir, channel, cancellationToken);
    }

    /// <summary>
    /// Creates .aspire/settings.json file with appHostPath configuration if an AppHost project is found.
    /// </summary>
    /// <param name="outputPath">The output directory path where the template was created.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task CreateSettingsFileAsync(string outputPath, CancellationToken cancellationToken)
    {
        try
        {
            var outputDir = new DirectoryInfo(outputPath);
            if (!outputDir.Exists)
            {
                return;
            }

            // Search for AppHost projects in the output directory
            var enumerationOptions = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                MaxRecursionDepth = 3 // Limit recursion to avoid deep traversal
            };

            FileInfo? appHostProject = null;

            // First, try to find .csproj files that are AppHost projects
            var projectFiles = outputDir.GetFiles("*.csproj", enumerationOptions);
            foreach (var projectFile in projectFiles)
            {
                var information = await runner.GetAppHostInformationAsync(
                    projectFile, 
                    new DotNetCliRunnerInvocationOptions(), 
                    cancellationToken);

                if (information.ExitCode == 0 && information.IsAspireHost)
                {
                    appHostProject = projectFile;
                    break;
                }
            }

            // If no .csproj AppHost found, try to find single-file apphost.cs
            if (appHostProject is null)
            {
                var appHostFiles = outputDir.GetFiles("apphost.cs", enumerationOptions);
                foreach (var candidateFile in appHostFiles)
                {
                    if (await IsValidSingleFileAppHostAsync(candidateFile, cancellationToken))
                    {
                        appHostProject = candidateFile;
                        break;
                    }
                }
            }

            // If an AppHost project was found, create the settings.json file
            if (appHostProject is not null)
            {
                var settingsFilePath = ConfigurationHelper.BuildPathToSettingsJsonFile(outputPath);
                var settingsFileDir = Path.GetDirectoryName(settingsFilePath);

                // Calculate relative path from settings file to AppHost project
                var relativePathToAppHost = Path.GetRelativePath(settingsFileDir!, appHostProject.FullName)
                    .Replace(Path.DirectorySeparatorChar, '/');

                // Create settings.json file with appHostPath
                var settings = new JsonObject
                {
                    ["appHostPath"] = relativePathToAppHost
                };

                // Ensure directory exists
                if (!Directory.Exists(settingsFileDir))
                {
                    Directory.CreateDirectory(settingsFileDir!);
                }

                // Write the settings file
                var jsonContent = JsonSerializer.Serialize(settings, JsonSourceGenerationContext.Default.JsonObject);
                await File.WriteAllTextAsync(settingsFilePath, jsonContent, cancellationToken);

                var relativeSettingsFilePath = Path.GetRelativePath(executionContext.WorkingDirectory.FullName, settingsFilePath)
                    .Replace(Path.DirectorySeparatorChar, '/');
                interactionService.DisplayMessage("file_cabinet", 
                    string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.CreatedSettingsFile, $"[bold]'{relativeSettingsFilePath}'[/]"));
            }
        }
        catch (Exception ex)
        {
            // If settings file creation fails, don't fail the entire operation
            // Just continue without creating the settings file
            interactionService.WriteConsoleLog($"Warning: Failed to create settings file: {ex.Message}", type: "warning");
        }
    }

    /// <summary>
    /// Checks whether the specified file is a valid Aspire single file app host.
    /// </summary>
    private static async Task<bool> IsValidSingleFileAppHostAsync(FileInfo candidateFile, CancellationToken cancellationToken)
    {
        // Check if file is named apphost.cs (case-insensitive)
        if (!candidateFile.Name.Equals("apphost.cs", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Check if directory contains no *.csproj files
        var siblingCsprojFiles = candidateFile.Directory!.EnumerateFiles("*.csproj", SearchOption.TopDirectoryOnly);
        if (siblingCsprojFiles.Any())
        {
            return false;
        }

        // Check for '#:sdk Aspire.AppHost.Sdk' directive
        try
        {
            using var reader = candidateFile.OpenText();
            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
            {
                var trimmedLine = line.TrimStart();
                if (trimmedLine.StartsWith("#:sdk Aspire.AppHost.Sdk", StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }
        catch (Exception)
        {
            // If we can't read the file, it's not a valid candidate
            // This can happen due to permissions, encoding issues, etc.
            return false;
        }

        return false;
    }
}

