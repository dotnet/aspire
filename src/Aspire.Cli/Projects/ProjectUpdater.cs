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

    var updateSteps = await interactionService.ShowStatusAsync(UpdateCommandStrings.AnalyzingProjectStatus, () => GetUpdateStepsAsync(projectFile, channel, cancellationToken));

        if (!updateSteps.Any())
        {
            logger.LogInformation("No updates required for project: {ProjectFile}", projectFile.FullName);
            return;
        }

    interactionService.DisplayMessage("check_mark", UpdateCommandStrings.ProjectHasUpdatesMessage);

        foreach (var updateStep in updateSteps)
        {
            // TODO: Replace this with a progress indicator or something.
            interactionService.DisplayMessage("package", updateStep.Description);
        }

        var result = await interactionService.PromptForSelectionAsync(
            UpdateCommandStrings.PerformUpdatesPrompt,
            [TemplatingStrings.Yes, TemplatingStrings.No],
            s => s,
            cancellationToken);

        if (result != TemplatingStrings.Yes)
        {
            return;
        }

        if (channel.Type == PackageChannelType.Explicit)
        {
            var (configPathsExitCode, configPaths) = await runner.GetNuGetConfigPathsAsync(projectFile.Directory!, new(), cancellationToken);

            if (configPathsExitCode != 0 || configPaths is null || configPaths.Length == 0)
            {
                throw new ProjectUpdaterException(UpdateCommandStrings.FailedDiscoverNuGetConfig);
            }

            var configPathDirectories = configPaths.Select(Path.GetDirectoryName).ToArray();
            var fallbackNuGetConfigDirectory = executionContext.WorkingDirectory.FullName;

            // If there is one or zero config paths we assume that we should use
            // the fallback (there should always be one, but just for exhaustivenss).
            // If there is more than one we just make sure that the first on in the list
            // isn't a global config (on Windows with .NET and VS installed you'll have 3
            // global config files but the first one should be the NuGet in AppData).
            // The final rule should never ever be invoked, its just to get around CS8846
            // which does not evaluate when statements for exhaustiveness.
            var recommendedNuGetConfigFileDirectory = configPathDirectories switch
            {
                { Length: 0 or 1 } => fallbackNuGetConfigDirectory,
                var p when p.Length > 1 => IsGlobalNuGetConfig(p[0]!) ? fallbackNuGetConfigDirectory : p[0],

                // CS8846 error if we don't put this rule here even though we do "when"
                // above - this is corner case in C# evalutation of switch statements.
                _ => throw new InvalidOperationException(UpdateCommandStrings.UnexpectedCodePath)
            };

            var selectedPathForNewNuGetConfigFile = await interactionService.PromptForStringAsync(
                promptText: UpdateCommandStrings.WhichDirectoryNuGetConfigPrompt,
                defaultValue: recommendedNuGetConfigFileDirectory,
                validator: null,
                isSecret: false,
                required: true,
                cancellationToken: cancellationToken);
                
            var nugetConfigDirectory = new DirectoryInfo(selectedPathForNewNuGetConfigFile);
            await NuGetConfigMerger.CreateOrUpdateAsync(nugetConfigDirectory, channel);
        }

        foreach (var updateStep in updateSteps)
        {
            interactionService.DisplaySubtleMessage(string.Format(System.Globalization.CultureInfo.InvariantCulture, UpdateCommandStrings.ExecutingUpdateStepFormat, updateStep.Description));
            await updateStep.Callback();
        }

        interactionService.DisplaySuccess(UpdateCommandStrings.UpdateSuccessfulMessage);
        return;
    }

    private static bool IsGlobalNuGetConfig(string path)
    {
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            return path.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        }
        else
        {
            var globalNuGetFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget");
            return path.StartsWith(globalNuGetFolder);
        }
    }

    private async Task<IEnumerable<UpdateStep>> GetUpdateStepsAsync(FileInfo projectFile, PackageChannel channel, CancellationToken cancellationToken)
    {
        var context = new UpdateContext(projectFile, channel);

    var appHostAnalyzeStep = new AnalyzeStep(UpdateCommandStrings.AnalyzeAppHost, () => AnalyzeAppHostAsync(context, cancellationToken));
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
            throw new ProjectUpdaterException(string.Format(System.Globalization.CultureInfo.InvariantCulture, UpdateCommandStrings.FailedFetchItemsAndPropertiesFormat, projectFile.FullName));
        }

        return document;
    }

    private Task AnalyzeAppHostAsync(UpdateContext context, CancellationToken cancellationToken)
    {
    var appHostSdkAnalyzeStep = new AnalyzeStep(UpdateCommandStrings.AnalyzeAppHostSdk, () => AnalyzeAppHostSdkAsync(context, cancellationToken));
        context.AnalyzeSteps.Enqueue(appHostSdkAnalyzeStep);

    var appHostProjectAnalyzeStep = new AnalyzeStep(string.Format(System.Globalization.CultureInfo.InvariantCulture, UpdateCommandStrings.AnalyzeProjectFormat, context.AppHostProjectFile.FullName), () => AnalyzeProjectAsync(context.AppHostProjectFile, context, cancellationToken));
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

    return latestPackage ?? throw new ProjectUpdaterException(string.Format(System.Globalization.CultureInfo.InvariantCulture, UpdateCommandStrings.NoPackageFoundFormat, packageId, context.Channel.Name));
    }

    private async Task AnalyzeAppHostSdkAsync(UpdateContext context, CancellationToken cancellationToken)
    {
        logger.LogDebug("Analyzing App Host SDK for: {AppHostFile}", context.AppHostProjectFile.FullName);

        var itemsAndPropertiesDocument = await GetItemsAndPropertiesAsync(context.AppHostProjectFile, cancellationToken);
        var propertiesElement = itemsAndPropertiesDocument.RootElement.GetProperty("Properties");
        var sdkVersionElement = propertiesElement.GetProperty("AspireHostingSDKVersion");

        var latestSdkPackage = await GetLatestVersionOfPackageAsync(context, "Aspire.AppHost.Sdk", cancellationToken);

        var sdkUpdateStep = new UpdateStep(
            string.Format(System.Globalization.CultureInfo.InvariantCulture, UpdateCommandStrings.UpdateAppHostSdkFormat, sdkVersionElement.GetString(), latestSdkPackage?.Version),
            () => UpdateSdkVersionInAppHostAsync(context.AppHostProjectFile, latestSdkPackage!));
        context.UpdateSteps.Enqueue(sdkUpdateStep);
    }

    private static async Task UpdateSdkVersionInAppHostAsync(FileInfo projectFile, NuGetPackageCli package)
    {
        var projectDocument = new XmlDocument();
        projectDocument.PreserveWhitespace = true;
        
        projectDocument.Load(projectFile.FullName);

        var projectNode = projectDocument.SelectSingleNode("/Project");
        if (projectNode is null)
        {
            throw new ProjectUpdaterException(string.Format(System.Globalization.CultureInfo.InvariantCulture, UpdateCommandStrings.CouldNotFindRootProjectElementFormat, projectFile.FullName));
        }

        var sdkNode = projectNode.SelectSingleNode("Sdk[@Name='Aspire.AppHost.Sdk']");
        if (sdkNode is null)
        {
            throw new ProjectUpdaterException(string.Format(System.Globalization.CultureInfo.InvariantCulture, UpdateCommandStrings.CouldNotFindSdkElementFormat, projectFile.FullName));
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
            var referencedProjectPath = projectReference.GetProperty("FullPath").GetString() ?? throw new ProjectUpdaterException(UpdateCommandStrings.ProjectReferenceNoFullPath);
            var referencedProjectFile = new FileInfo(referencedProjectPath);
            context.AnalyzeSteps.Enqueue(new AnalyzeStep(string.Format(System.Globalization.CultureInfo.InvariantCulture, UpdateCommandStrings.AnalyzeProjectFormat, referencedProjectFile.FullName), () => AnalyzeProjectAsync(referencedProjectFile, context, cancellationToken)));
        }

        var packageReferencesElement = itemsElement.GetProperty("PackageReference").EnumerateArray();
        foreach (var packageReference in packageReferencesElement)
        {
            var packageId = packageReference.GetProperty("Identity").GetString() ?? throw new ProjectUpdaterException(UpdateCommandStrings.PackageReferenceNoIdentity);

            if (!IsUpdatablePackage(packageId))
            {
                continue;
            }

            var packageVersion = packageReference.GetProperty("Version").GetString() ?? throw new ProjectUpdaterException(UpdateCommandStrings.PackageReferenceNoVersion);
            var latestPackage = await GetLatestVersionOfPackageAsync(context, packageId, cancellationToken);

            var updateStep = new UpdateStep(
                string.Format(System.Globalization.CultureInfo.InvariantCulture, UpdateCommandStrings.UpdatePackageFormat, packageId, packageVersion, latestPackage.Version),
                () => UpdatePackageReferenceInProject(projectFile, latestPackage, context, cancellationToken));
            context.UpdateSteps.Enqueue(updateStep);
        }
    }

    private static bool IsUpdatablePackage(string packageId)
    {
        return packageId.StartsWith("Aspire.")
            || packageId.StartsWith("Microsoft.Extensions.ServiceDiscovery.")
            || packageId.Equals("Microsoft.Extensions.ServiceDiscovery");
    }

    private async Task UpdatePackageReferenceInProject(FileInfo projectFile, NuGetPackageCli package, UpdateContext context, CancellationToken cancellationToken)
    {
        _ = context;
        var exitCode = await runner.AddPackageAsync(
            projectFilePath: projectFile,
            packageName: package.Id,
            packageVersion: package.Version,
            nugetSource: null, // When source is null we apped --no-restore.
            options: new(),
            cancellationToken: cancellationToken);

        if (exitCode != 0)
        {
            throw new ProjectUpdaterException(string.Format(System.Globalization.CultureInfo.InvariantCulture, UpdateCommandStrings.FailedUpdatePackageReferenceFormat, package.Id, projectFile.FullName));
        }
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