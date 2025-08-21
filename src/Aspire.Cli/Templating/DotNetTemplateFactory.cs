// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using Aspire.Cli.Certificates;
using Aspire.Cli.Commands;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.Packaging;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using NuGetPackage = Aspire.Shared.NuGetPackageCli;
using System.Xml.Linq;
using Semver;

namespace Aspire.Cli.Templating;

internal class DotNetTemplateFactory(IInteractionService interactionService, IDotNetCliRunner runner, ICertificateService certificateService, IPackagingService packagingService, INewCommandPrompter prompter, CliExecutionContext executionContext) : ITemplateFactory
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
            (template, parseResult, ct) => ApplyTemplateAsync(template, parseResult, PromptForExtraAspireXUnitOptionsAsync, ct)
            );

        // Prepends a test framework selection step then calls the
        // underlying test template.
        yield return new CallbackTemplate(
            "aspire-test",
            TemplatingStrings.IntegrationTestsTemplate_Description,
            projectName => $"./{projectName}",
            _ => { },
            async (template, parseResult, ct) =>
            {
                var testTemplate = await prompter.PromptForTemplateAsync(
                    [msTestTemplate, xunitTemplate, nunitTemplate],
                    ct
                );

                var testCallbackTemplate = (CallbackTemplate)testTemplate;
                return await testCallbackTemplate.ApplyTemplateAsync(parseResult, ct);
            });
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
            interactionService.DisplayMessage("check_mark", TemplatingStrings.UseRedisCache_UsingRedisCache);
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

            interactionService.DisplayMessage("check_mark", string.Format(CultureInfo.CurrentCulture, TemplatingStrings.PromptForTFM_UsingForTesting, testFramework));

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
            var selectedTemplateDetails = await GetProjectTemplatesVersionAsync(parseResult, cancellationToken: cancellationToken);
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
                // Exit code 73 indicates that the output directory already contains files from a previous project
                // See: https://github.com/dotnet/aspire/issues/9685
                if (newProjectExitCode == 73)
                {
                    interactionService.DisplayError(TemplatingStrings.ProjectAlreadyExists);
                    return new TemplateResult(ExitCodeConstants.FailedToCreateNewProject);
                }

                interactionService.DisplayLines(newProjectCollector.GetLines());
                interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, TemplatingStrings.ProjectCreationFailed, newProjectExitCode));
                return new TemplateResult(ExitCodeConstants.FailedToCreateNewProject);
            }

            await certificateService.EnsureCertificatesTrustedAsync(runner, cancellationToken);

            // For explicit channels, optionally create or update a NuGet.config. If none exists in the current
            // working directory, create one in the newly created project's output directory.
            await PromptToCreateOrUpdateNuGetConfigAsync(selectedTemplateDetails.Channel, temporaryConfig, outputPath, cancellationToken);

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
            var defaultName = executionContext.WorkingDirectory.Name;
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

    private async Task<(NuGetPackage Package, PackageChannel Channel)> GetProjectTemplatesVersionAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        _ = parseResult;
        var channels = await packagingService.GetChannelsAsync(cancellationToken);

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

        if (parseResult.GetValue<string>("--version") is { } version)
        {
            var explicitPacakgeFromChannel = orderedPackagesFromChannels.FirstOrDefault(p => p.Package.Version == version);
            var explicitPackageFromChannel = orderedPackagesFromChannels.FirstOrDefault(p => p.Package.Version == version);
            return explicitPackageFromChannel;
        }

        var selectedPackageFromChannel = await prompter.PromptForTemplatesVersionAsync(orderedPackagesFromChannels, cancellationToken);
        return selectedPackageFromChannel;
    }

    private async Task PromptToCreateOrUpdateNuGetConfigAsync(PackageChannel channel, TemporaryNuGetConfig? temporaryConfig, string outputPath, CancellationToken cancellationToken)
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
        // Locate an existing NuGet.config in the current directory using a case-insensitive search
        var nugetConfigFile = TryFindNuGetConfigInDirectory(workingDir);

        // We only act if we need to create or update
        if (nugetConfigFile is null)
        {
            // Ask for confirmation before creating the file
            var choice = await interactionService.PromptForSelectionAsync(
                TemplatingStrings.CreateNugetConfigConfirmation,
                [TemplatingStrings.Yes, TemplatingStrings.No],
                c => c,
                cancellationToken);

            if (string.Equals(choice, TemplatingStrings.Yes, StringComparisons.CliInputOrOutput))
            {
                // Use the temporary config we already generated; if it's missing, generate a fresh one
                if (temporaryConfig is null)
                {
                    using var tmpConfig = await TemporaryNuGetConfig.CreateAsync(mappings);
                    var outputDir = new DirectoryInfo(outputPath);
                    Directory.CreateDirectory(outputDir.FullName);
                    var targetPath = Path.Combine(outputDir.FullName, "NuGet.config");
                    File.Copy(tmpConfig.ConfigFile.FullName, targetPath, overwrite: true);
                }
                else
                {
                    // Ensure target directory exists
                    var outputDir = new DirectoryInfo(outputPath);
                    Directory.CreateDirectory(outputDir.FullName);
                    var targetPath = Path.Combine(outputDir.FullName, "NuGet.config");
                    File.Copy(temporaryConfig.ConfigFile.FullName, targetPath, overwrite: true);
                }
                interactionService.DisplayMessage("package", TemplatingStrings.NuGetConfigCreatedConfirmationMessage);
            }

            return;
        }

        // Update existing NuGet.config if any of the required sources are missing
        var requiredSources = mappings
            .Select(m => m.Source)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        XDocument doc;
    await using (var stream = nugetConfigFile.OpenRead())
        {
            doc = XDocument.Load(stream);
        }

        var configuration = doc.Root ?? new XElement("configuration");
        if (doc.Root is null)
        {
            doc.Add(configuration);
        }

        var packageSources = configuration.Element("packageSources");
        if (packageSources is null)
        {
            packageSources = new XElement("packageSources");
            configuration.Add(packageSources);
        }

        var existingAdds = packageSources.Elements("add").ToArray();
        var existingValues = new HashSet<string>(existingAdds
            .Select(e => (string?)e.Attribute("value") ?? string.Empty), StringComparer.OrdinalIgnoreCase);
        var existingKeys = new HashSet<string>(existingAdds
            .Select(e => (string?)e.Attribute("key") ?? string.Empty), StringComparer.OrdinalIgnoreCase);

        var missingSources = requiredSources
            .Where(s => !existingValues.Contains(s) && !existingKeys.Contains(s))
            .ToArray();

        if (missingSources.Length == 0)
        {
            return;
        }

        var updateChoice = await interactionService.PromptForSelectionAsync(
            "Update NuGet.config to add missing package sources for the selected channel?",
            [TemplatingStrings.Yes, TemplatingStrings.No],
            c => c,
            cancellationToken);

        if (!string.Equals(updateChoice, TemplatingStrings.Yes, StringComparisons.CliInputOrOutput))
        {
            return;
        }

        foreach (var source in missingSources)
        {
            // Use the source URL as both key and value for consistency with our temporary config
            var add = new XElement("add");
            add.SetAttributeValue("key", source);
            add.SetAttributeValue("value", source);
            packageSources.Add(add);
        }

        // Save back the updated document
        await using (var writeStream = nugetConfigFile.Open(FileMode.Create, FileAccess.Write, FileShare.None))
        {
            doc.Save(writeStream);
        }

        interactionService.DisplayMessage("package", "Updated NuGet.config with required package sources.");
    }

    private static FileInfo? TryFindNuGetConfigInDirectory(DirectoryInfo directory)
    {
        ArgumentNullException.ThrowIfNull(directory);

        // Search only the specified directory for a file named "nuget.config", ignoring case
        return directory
            .EnumerateFiles("*", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(f => string.Equals(f.Name, "nuget.config", StringComparison.OrdinalIgnoreCase));
    }
}

