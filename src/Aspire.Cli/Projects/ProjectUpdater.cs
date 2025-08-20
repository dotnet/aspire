// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Text.Json;
using System.Xml;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.Packaging;
using Aspire.Cli.Resources;
using Aspire.Shared;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Semver;

namespace Aspire.Cli.Projects;

internal interface IProjectUpdater
{
    Task UpdateProjectAsync(FileInfo projectFile, PackageChannel channel, CancellationToken cancellationToken);
}

internal sealed class ProjectUpdater(ILogger<ProjectUpdater> logger, IDotNetCliRunner runner, IInteractionService interactionService, IMemoryCache cache, CliExecutionContext executionContext) : IProjectUpdater
{
    public async Task UpdateProjectAsync(FileInfo projectFile, PackageChannel channel, CancellationToken cancellationToken)
    {
        logger.LogDebug("Fetching '{AppHostPath}' items and properties.", projectFile.FullName);

        var updateSteps = await interactionService.ShowStatusAsync("Analyzing project...", () => GetUpdateStepsAsync(projectFile, channel, cancellationToken));

        if (!updateSteps.Any())
        {
            logger.LogInformation("No updates required for project: {ProjectFile}", projectFile.FullName);
            return;
        }

        interactionService.DisplayMessage("check_mark", "Project has updates!");

        foreach (var updateStep in updateSteps)
        {
            // TODO: Replace this with a progress indicator or something.
            interactionService.DisplayMessage("package", updateStep.Description);
        }

        var result = await interactionService.PromptForSelectionAsync(
            "Perform updates?",
            [TemplatingStrings.Yes, TemplatingStrings.No],
            s => s,
            cancellationToken);

        if (result != TemplatingStrings.Yes)
        {
            return;
        }

        foreach (var updateStep in updateSteps)
        {
            interactionService.DisplaySubtleMessage($"Executing: {updateStep.Description}");
            await updateStep.Callback();
        }

        if (channel.Type == PackageChannelType.Explicit)
        {
            // If we are using an explicit channel we may need to update the config
            // file, however, unlike "aspire new" we can't just place the file in the
            // output path - we need to find it.
            var shouldUpdateNuGet = await interactionService.PromptForSelectionAsync(
                "Update NuGet.config?",
                [TemplatingStrings.Yes, TemplatingStrings.No],
                s => s,
                cancellationToken);

            if (shouldUpdateNuGet != TemplatingStrings.Yes)
            {
                return;
            }

            var (configPathsExitCode, configPaths) = await runner.GetNuGetConfigPathsAsync(projectFile.Directory!, new(), cancellationToken);

            if (configPathsExitCode != 0 || configPaths is null || configPaths.Length == 0)
            {
                throw new ProjectUpdaterException($"Failed to discover NuGet.config files.");
            }

            // If there is only one NuGet.config path that
            // means it is the global path. In that case we
            // need to prompt to confirm WHERE to put the NuGet.config
            // file. This is because the file needs to be placed
            // somewhere that the builds for projects can find it.
            //
            // We are going to take a guess that the current working
            // directory for the CLI invocation is a reasonable default
            // location for the NuGet.config file, but we'll prompt the
            // user with that as the path to confirm that.
            //
            // If we have more than one config file then we assume that
            // the user wants the first one, but we'll prompt later to
            // confirm.
            var recommendedNuGetConfigFileDirectory = configPaths switch
            {
                { Length: 1 } => executionContext.WorkingDirectory.FullName,
                { Length: > 1 } => configPaths[0],
                _ => throw new ProjectUpdaterException("No NuGet.config files listed.")
            };

            var selectedPathForNewNuGetConfigFile = await interactionService.PromptForStringAsync(
                promptText: "Which directory for NuGet.config file?",
                defaultValue: recommendedNuGetConfigFileDirectory,
                validator: null,
                isSecret: false,
                required: true,
                cancellationToken: cancellationToken);
                
            var nugetConfigDirectory = new DirectoryInfo(selectedPathForNewNuGetConfigFile);
            using var tempConfig = await TemporaryNuGetConfig.CreateAsync(channel.Mappings!);
            await NuGetConfigMerger.CreateOrUpdateAsync(nugetConfigDirectory, tempConfig, channel.Mappings);

            interactionService.DisplaySuccess("Update successful!");
            return;
        }
    }

    private async Task<IEnumerable<UpdateStep>> GetUpdateStepsAsync(FileInfo projectFile, PackageChannel channel, CancellationToken cancellationToken)
    {
        var context = new UpdateContext(projectFile, channel);

        var appHostAnalyzeStep = new AnalyzeStep("Analyze App Host", () => AnalyzeAppHostAsync(context, cancellationToken));
        context.AnalyzeSteps.Enqueue(appHostAnalyzeStep);

        while (context.AnalyzeSteps.TryDequeue(out var analyzeStep))
        {
            await analyzeStep.Callback();
        }

        return context.UpdateSteps;
    }

    private const string ItemsAndPropertiesCacheKeyPrefix = "ItemsAndProperties";

    private async Task<JsonDocument> GetItemsAndPropertiesAsync(FileInfo projectFile, CancellationToken cancellationToken)
    {
        var cacheKey = $"{ItemsAndPropertiesCacheKeyPrefix}_{projectFile.FullName}";
        var (exitCode, document) = await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            return await runner.GetProjectItemsAndPropertiesAsync(projectFile, ["PackageReference", "ProjectReference"], ["AspireHostingSDKVersion"], new(), cancellationToken);
        });

        if (exitCode != 0 || document is null)
        {
            throw new ProjectUpdaterException($"Failed to fetch items and properties for project: {projectFile.FullName}");
        }

        return document;
    }

    private Task AnalyzeAppHostAsync(UpdateContext context, CancellationToken cancellationToken)
    {
        var appHostSdkAnalyzeStep = new AnalyzeStep("Analyze App Host SDK", () => AnalyzeAppHostSdkAsync(context, cancellationToken));
        context.AnalyzeSteps.Enqueue(appHostSdkAnalyzeStep);

        var appHostProjectAnalyzeStep = new AnalyzeStep($"Analyze project: {context.AppHostProjectFile.FullName}", () => AnalyzeProjectAsync(context.AppHostProjectFile, context, cancellationToken));
        context.AnalyzeSteps.Enqueue(appHostProjectAnalyzeStep);

        return Task.CompletedTask;
    }

    private async Task<NuGetPackageCli> GetLatestVersionOfPackageAsync(UpdateContext context, string packageId, CancellationToken cancellationToken)
    {
        var cacheKey = $"LatestPackage-{packageId}";
        var latestPackage = await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            var packages = await context.Channel.GetPackagesAsync(packageId, context.AppHostProjectFile.Directory!, cancellationToken);
            var latestPackage = packages.OrderByDescending(p => SemVersion.Parse(p.Version), SemVersion.PrecedenceComparer).FirstOrDefault();
            return latestPackage;
        });

        return latestPackage ?? throw new ProjectUpdaterException($"No package found with ID '{packageId}' in channel '{context.Channel.Name}'.");
    }

    private async Task AnalyzeAppHostSdkAsync(UpdateContext context, CancellationToken cancellationToken)
    {
        logger.LogDebug("Analyzing App Host SDK for: {AppHostFile}", context.AppHostProjectFile.FullName);

        var itemsAndPropertiesDocument = await GetItemsAndPropertiesAsync(context.AppHostProjectFile, cancellationToken);
        var propertiesElement = itemsAndPropertiesDocument.RootElement.GetProperty("Properties");
        var sdkVersionElement = propertiesElement.GetProperty("AspireHostingSDKVersion");

        var latestSdkPackage = await GetLatestVersionOfPackageAsync(context, "Aspire.AppHost.Sdk", cancellationToken);

        if (SemVersion.Parse(latestSdkPackage.Version).ComparePrecedenceTo(SemVersion.Parse(sdkVersionElement.GetString()!)) > 0)
        {
            var sdkUpdateStep = new UpdateStep(
                $"Update AppHost SDK from {sdkVersionElement.GetString()} to {latestSdkPackage?.Version}",
                () => UpdateSdkVersionInAppHostAsync(context.AppHostProjectFile, latestSdkPackage!));
            context.UpdateSteps.Enqueue(sdkUpdateStep);
        }
    }

    private static async Task UpdateSdkVersionInAppHostAsync(FileInfo projectFile, NuGetPackageCli package)
    {
        var projectDocument = new XmlDocument();
        projectDocument.PreserveWhitespace = true;
        
        projectDocument.Load(projectFile.FullName);

        var projectNode = projectDocument.SelectSingleNode("/Project");
        if (projectNode is null)
        {
            throw new ProjectUpdaterException($"Could not find root <Project> element in {projectFile.FullName}");
        }

        var sdkNode = projectNode.SelectSingleNode("Sdk[@Name='Aspire.AppHost.Sdk']");
        if (sdkNode is null)
        {
            throw new ProjectUpdaterException($"Could not find <Sdk Name='Aspire.AppHost.Sdk' /> element in {projectFile.FullName}");
        }
        
        sdkNode.Attributes?["Version"]?.Value = package.Version;

        projectDocument.Save(projectFile.FullName);

        await Task.CompletedTask;
    }

    private async Task AnalyzeProjectAsync(FileInfo projectFile, UpdateContext context, CancellationToken cancellationToken)
    {
        var itemsAndPropertiesDocument = await GetItemsAndPropertiesAsync(projectFile, cancellationToken);
        var itemsElement = itemsAndPropertiesDocument.RootElement.GetProperty("Items");

        var projectReferencesElement = itemsElement.GetProperty("ProjectReference").EnumerateArray();
        foreach (var projectReference in projectReferencesElement)
        {
            var referencedProjectPath = projectReference.GetProperty("FullPath").GetString() ?? throw new ProjectUpdaterException("Project reference does not have FullPath.");
            var referencedProjectFile = new FileInfo(referencedProjectPath);
            context.AnalyzeSteps.Enqueue(new AnalyzeStep($"Analyze project: {referencedProjectFile.FullName}", () => AnalyzeProjectAsync(referencedProjectFile, context, cancellationToken)));
        }

        var packageReferencesElement = itemsElement.GetProperty("PackageReference").EnumerateArray();
        foreach (var packageReference in packageReferencesElement)
        {
            var packageId = packageReference.GetProperty("Identity").GetString() ?? throw new ProjectUpdaterException("Package reference does not have Identity.");

            if (!packageId.StartsWith("Aspire."))
            {
                // We only look at Aspire packages here!
                continue;
            }

            var packageVersion = packageReference.GetProperty("Version").GetString() ?? throw new ProjectUpdaterException("Package reference does not have Version.");
            var latestPackage = await GetLatestVersionOfPackageAsync(context, packageId, cancellationToken);

            if (SemVersion.Parse(latestPackage.Version).ComparePrecedenceTo(SemVersion.Parse(packageVersion)) > 0)
            {
                var updateStep = new UpdateStep(
                    $"Update package {packageId} from {packageVersion} to {latestPackage.Version}",
                    () => UpdatePackageReferenceInProject(projectFile, latestPackage, context, cancellationToken));
                context.UpdateSteps.Enqueue(updateStep);
            }
        }
    }

    private async Task UpdatePackageReferenceInProject(FileInfo projectFile, NuGetPackageCli package, UpdateContext context, CancellationToken cancellationToken)
    {
        _ = context;
        await runner.AddPackageAsync(projectFile, package.Id, package.Version, package.Source, new(), cancellationToken);
    }
}

internal sealed class UpdateContext(FileInfo appHostProjectFile, PackageChannel channel)
{
    public FileInfo AppHostProjectFile { get; } = appHostProjectFile;
    public PackageChannel Channel { get; } = channel;
    public ConcurrentQueue<UpdateStep> UpdateSteps { get; } = new();
    public ConcurrentQueue<AnalyzeStep> AnalyzeSteps { get; } = new();
}

internal record UpdateStep(string Description, Func<Task> Callback)
{
}

internal record AnalyzeStep(string Description, Func<Task> Callback)
{
    
}

internal sealed class ProjectUpdaterException : System.Exception
{
    public ProjectUpdaterException(string message) : base(message) { }
    public ProjectUpdaterException(string message, System.Exception inner) : base(message, inner) { }
}