// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Text.Json;
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

internal sealed class ProjectUpdater(ILogger<ProjectUpdater> logger, IDotNetCliRunner runner, IInteractionService interactionService, IMemoryCache cache) : IProjectUpdater
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

        if (result == TemplatingStrings.Yes)
        {
            foreach (var updateStep in updateSteps)
            {
                interactionService.DisplaySubtleMessage($"Executing: {updateStep.Description}");
                await updateStep.Callback();
            }
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
        // The AppHost project itself is special because we need to update the SDK.
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

        // TODO: Add logic to actually do update.
        var sdkUpdateStep = new UpdateStep(
            $"Update AppHost SDK from {sdkVersionElement.GetString()} to {latestSdkPackage?.Version}",
            () => Task.CompletedTask);
        context.UpdateSteps.Enqueue(sdkUpdateStep);
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

    private static async Task UpdatePackageReferenceInProject(FileInfo projectFile, NuGetPackageCli package, UpdateContext context, CancellationToken cancellationToken)
    {
        // TODO: Wire up cliRunner to do dotnet package update
        _ = projectFile;
        _ = package;
        _ = context;
        _ = cancellationToken;
        await Task.Delay(1000, cancellationToken);
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