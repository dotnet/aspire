// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Globalization;
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
using Spectre.Console;

namespace Aspire.Cli.Projects;

internal interface IProjectUpdater
{
    Task<ProjectUpdateResult> UpdateProjectAsync(FileInfo projectFile, PackageChannel channel, CancellationToken cancellationToken = default);
}

internal sealed class ProjectUpdater(ILogger<ProjectUpdater> logger, IDotNetCliRunner runner, IInteractionService interactionService, IMemoryCache cache, CliExecutionContext executionContext, FallbackProjectParser fallbackParser) : IProjectUpdater
{
    public async Task<ProjectUpdateResult> UpdateProjectAsync(FileInfo projectFile, PackageChannel channel, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Fetching '{AppHostPath}' items and properties.", projectFile.FullName);

        var (updateSteps, fallbackUsed) = await interactionService.ShowStatusAsync(UpdateCommandStrings.AnalyzingProjectStatus, () => GetUpdateStepsAsync(projectFile, channel, cancellationToken));

        if (!updateSteps.Any())
        {
            logger.LogInformation("No updates required for project: {ProjectFile}", projectFile.FullName);
            interactionService.DisplayMessage("check_mark", UpdateCommandStrings.ProjectUpToDateMessage);
            return new ProjectUpdateResult { UpdatedApplied = false };
        }

        interactionService.DisplayEmptyLine();

        // Group update steps by project for better visual organization
        var updateStepsByProject = updateSteps
            .OfType<PackageUpdateStep>()
            .GroupBy(step => step.ProjectFile.FullName)
            .ToList();

        // Display package updates grouped by project
        foreach (var projectGroup in updateStepsByProject)
        {
            var projectName = new FileInfo(projectGroup.Key).Name;
            if (updateStepsByProject.Count > 1)
            {
                interactionService.DisplayMessage("file_folder", $"[bold cyan]{projectName}[/]:");
            }

            foreach (var packageStep in projectGroup)
            {
                interactionService.DisplayMessage("package", packageStep.GetFormattedDisplayText());
            }

            interactionService.DisplayEmptyLine();
        }

        // Display warning if fallback XML parsing was used
        if (fallbackUsed)
        {
            interactionService.DisplayMessage("warning", "[yellow]Note: Update plan generated using fallback XML parsing due to unresolvable AppHost SDK. Dependency analysis may have reduced accuracy.[/]");
            interactionService.DisplayEmptyLine();
        }

        if (!await interactionService.ConfirmAsync(UpdateCommandStrings.PerformUpdatesPrompt, true, cancellationToken))
        {
            return new ProjectUpdateResult { UpdatedApplied = false };
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

            interactionService.DisplayEmptyLine();

            var selectedPathForNewNuGetConfigFile = await interactionService.PromptForStringAsync(
                promptText: UpdateCommandStrings.WhichDirectoryNuGetConfigPrompt,
                defaultValue: recommendedNuGetConfigFileDirectory,
                validator: null,
                isSecret: false,
                required: true,
                cancellationToken: cancellationToken);

            var nugetConfigDirectory = new DirectoryInfo(selectedPathForNewNuGetConfigFile);
            await NuGetConfigMerger.CreateOrUpdateAsync(nugetConfigDirectory, channel, AnalyzeAndConfirmNuGetConfigChanges, cancellationToken);
        }

        interactionService.DisplayEmptyLine();

        foreach (var updateStep in updateSteps)
        {
            interactionService.DisplaySubtleMessage(string.Format(System.Globalization.CultureInfo.InvariantCulture, UpdateCommandStrings.ExecutingUpdateStepFormat, updateStep.Description));
            await updateStep.Callback();
        }

        interactionService.DisplayEmptyLine();

        interactionService.DisplaySuccess(UpdateCommandStrings.UpdateSuccessfulMessage);
        return new ProjectUpdateResult { UpdatedApplied = true };
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

    private async Task<(IEnumerable<UpdateStep> UpdateSteps, bool FallbackUsed)> GetUpdateStepsAsync(FileInfo projectFile, PackageChannel channel, CancellationToken cancellationToken)
    {
        var context = new UpdateContext(projectFile, channel);

        var appHostAnalyzeStep = new AnalyzeStep(UpdateCommandStrings.AnalyzeAppHost, () => AnalyzeAppHostAsync(context, cancellationToken));
        context.AnalyzeSteps.Enqueue(appHostAnalyzeStep);

        while (context.AnalyzeSteps.TryDequeue(out var analyzeStep))
        {
            await analyzeStep.Callback();
        }

        return (context.UpdateSteps, context.FallbackXmlParsing);
    }

    private const string ItemsAndPropertiesCacheKeyPrefix = "ItemsAndProperties";

    private async Task<JsonDocument> GetItemsAndPropertiesAsync(FileInfo projectFile, CancellationToken cancellationToken)
    {
        return await GetItemsAndPropertiesAsync(projectFile, ["PackageReference", "ProjectReference"], ["AspireHostingSDKVersion"], cancellationToken);
    }

    private async Task<JsonDocument> GetItemsAndPropertiesAsync(FileInfo projectFile, string[] items, string[] properties, CancellationToken cancellationToken)
    {
        // Create a cache key that includes the project file and the requested items/properties
        var itemsKey = string.Join(",", items.OrderBy(x => x));
        var propertiesKey = string.Join(",", properties.OrderBy(x => x));
        var cacheKey = $"{ItemsAndPropertiesCacheKeyPrefix}_{projectFile.FullName}_{itemsKey}_{propertiesKey}";
        
        var (exitCode, document) = await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            return await runner.GetProjectItemsAndPropertiesAsync(projectFile, items, properties, new(), cancellationToken);
        });

        if (exitCode != 0 || document is null)
        {
            throw new ProjectUpdaterException(string.Format(System.Globalization.CultureInfo.InvariantCulture, UpdateCommandStrings.FailedFetchItemsAndPropertiesFormat, projectFile.FullName));
        }

        return document;
    }

    private async Task<JsonDocument> GetItemsAndPropertiesWithFallbackAsync(FileInfo projectFile, UpdateContext context, CancellationToken cancellationToken)
    {
        return await GetItemsAndPropertiesWithFallbackAsync(projectFile, ["PackageReference", "ProjectReference"], ["AspireHostingSDKVersion"], context, cancellationToken);
    }

    private async Task<JsonDocument> GetItemsAndPropertiesWithFallbackAsync(FileInfo projectFile, string[] items, string[] properties, UpdateContext context, CancellationToken cancellationToken)
    {
        try
        {
            // Try normal MSBuild evaluation first
            return await GetItemsAndPropertiesAsync(projectFile, items, properties, cancellationToken);
        }
        catch (ProjectUpdaterException ex) when (IsAppHostProject(projectFile, context))
        {
            // Only use fallback for AppHost projects
            logger.LogWarning("Falling back to XML parsing for '{ProjectFile}'. Reason: {Message}", projectFile.FullName, ex.Message);
            
            if (!context.FallbackXmlParsing)
            {
                context.FallbackXmlParsing = true;
                logger.LogWarning("Update plan will be generated using fallback XML parsing; dependency accuracy may be reduced.");
            }

            return fallbackParser.ParseProject(projectFile);
        }
    }

    private static bool IsAppHostProject(FileInfo projectFile, UpdateContext context)
    {
        return string.Equals(projectFile.FullName, context.AppHostProjectFile.FullName, StringComparison.OrdinalIgnoreCase);
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

        var itemsAndPropertiesDocument = await GetItemsAndPropertiesWithFallbackAsync(context.AppHostProjectFile, context, cancellationToken);
        var propertiesElement = itemsAndPropertiesDocument.RootElement.GetProperty("Properties");
        
        // Check if AspireHostingSDKVersion property exists - it might not exist in legacy 8.x projects
        string? currentSdkVersion = null;
        bool needsSdkElement = false;
        
        if (propertiesElement.TryGetProperty("AspireHostingSDKVersion", out var sdkVersionElement))
        {
            currentSdkVersion = sdkVersionElement.GetString();
        }
        else
        {
            // Legacy 8.x project without SDK element - needs to be injected
            logger.LogInformation("Legacy AppHost project detected without SDK element. Will inject Aspire.AppHost.Sdk element.");
            needsSdkElement = true;
            currentSdkVersion = null; // Treat as if no version is present
        }

        var latestSdkPackage = await GetLatestVersionOfPackageAsync(context, "Aspire.AppHost.Sdk", cancellationToken);

        if (currentSdkVersion == latestSdkPackage?.Version && !needsSdkElement)
        {
            logger.LogInformation("App Host SDK is up to date.");
            return;
        }

        var sdkUpdateStep = new PackageUpdateStep(
            string.Format(System.Globalization.CultureInfo.InvariantCulture, UpdateCommandStrings.UpdatePackageFormat, "Aspire.AppHost.Sdk", currentSdkVersion ?? "none", latestSdkPackage?.Version),
            () => UpdateSdkVersionInAppHostAsync(context.AppHostProjectFile, latestSdkPackage!, needsSdkElement),
            "Aspire.AppHost.Sdk",
            currentSdkVersion ?? "none",
            latestSdkPackage?.Version ?? "unknown",
            context.AppHostProjectFile);
        context.UpdateSteps.Enqueue(sdkUpdateStep);
    }

    private static async Task UpdateSdkVersionInAppHostAsync(FileInfo projectFile, NuGetPackageCli package, bool needsSdkElement = false)
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
            if (needsSdkElement)
            {
                // Create and inject the new SDK element with proper formatting
                
                // Create the SDK element
                sdkNode = projectDocument.CreateElement("Sdk");
                var nameAttribute = projectDocument.CreateAttribute("Name");
                nameAttribute.Value = "Aspire.AppHost.Sdk";
                sdkNode.Attributes!.Append(nameAttribute);
                
                var versionAttribute = projectDocument.CreateAttribute("Version");
                versionAttribute.Value = package.Version;
                sdkNode.Attributes!.Append(versionAttribute);
                
                // Find the first element child to insert before
                XmlNode? firstElementChild = null;
                foreach (XmlNode child in projectNode.ChildNodes)
                {
                    if (child.NodeType == XmlNodeType.Element)
                    {
                        firstElementChild = child;
                        break;
                    }
                }

                if (firstElementChild is not null)
                {
                    // Insert the SDK element before the first element child
                    projectNode.InsertBefore(sdkNode, firstElementChild);
                    
                    // Add newline and indentation after the SDK element
                    var whitespaceAfterSdk = projectDocument.CreateTextNode("\n    ");
                    projectNode.InsertBefore(whitespaceAfterSdk, firstElementChild);
                }
                else
                {
                    // No element children, just append with proper formatting
                    var whitespaceBeforeSdk = projectDocument.CreateTextNode("\n    ");
                    projectNode.AppendChild(whitespaceBeforeSdk);
                    projectNode.AppendChild(sdkNode);
                    var whitespaceAfterSdk = projectDocument.CreateTextNode("\n");
                    projectNode.AppendChild(whitespaceAfterSdk);
                }
            }
            else
            {
                throw new ProjectUpdaterException(string.Format(System.Globalization.CultureInfo.InvariantCulture, UpdateCommandStrings.CouldNotFindSdkElementFormat, projectFile.FullName));
            }
        }
        else
        {
            // Update existing SDK element version
            sdkNode.Attributes?["Version"]!.Value = package.Version;
        }

        projectDocument.Save(projectFile.FullName);

        await Task.CompletedTask;
    }

    private async Task AnalyzeProjectAsync(FileInfo projectFile, UpdateContext context, CancellationToken cancellationToken)
    {
        if (!context.VisitedProjects.Add(projectFile.FullName))
        {
            // Project already analyzed, skip
            return;
        }

        // Detect if this project uses Central Package Management
        var cpmInfo = DetectCentralPackageManagement(projectFile);

        // Use fallback wrapper for AppHost project, normal method for others
        var itemsAndPropertiesDocument = IsAppHostProject(projectFile, context)
            ? await GetItemsAndPropertiesWithFallbackAsync(projectFile, context, cancellationToken)
            : await GetItemsAndPropertiesAsync(projectFile, cancellationToken);

        var itemsElement = itemsAndPropertiesDocument.RootElement.GetProperty("Items");

        // Handle ProjectReference items (may not exist if project has no project references)
        if (itemsElement.TryGetProperty("ProjectReference", out var projectReferencesElement))
        {
            foreach (var projectReference in projectReferencesElement.EnumerateArray())
            {
                var referencedProjectPath = projectReference.GetProperty("FullPath").GetString() ?? throw new ProjectUpdaterException(UpdateCommandStrings.ProjectReferenceNoFullPath);
                var referencedProjectFile = new FileInfo(referencedProjectPath);
                context.AnalyzeSteps.Enqueue(new AnalyzeStep(string.Format(System.Globalization.CultureInfo.InvariantCulture, UpdateCommandStrings.AnalyzeProjectFormat, referencedProjectFile.FullName), () => AnalyzeProjectAsync(referencedProjectFile, context, cancellationToken)));
            }
        }

        // Handle PackageReference items (may not exist if project has no package references)
        if (itemsElement.TryGetProperty("PackageReference", out var packageReferencesElement))
        {
            foreach (var packageReference in packageReferencesElement.EnumerateArray())
            {
            var packageId = packageReference.GetProperty("Identity").GetString() ?? throw new ProjectUpdaterException(UpdateCommandStrings.PackageReferenceNoIdentity);

            if (!IsUpdatablePackage(packageId))
            {
                continue;
            }

            if (cpmInfo.UsesCentralPackageManagement)
            {
                await AnalyzePackageForCentralPackageManagementAsync(packageId, projectFile, cpmInfo.DirectoryPackagesPropsFile!, context, cancellationToken);
            }
            else
            {
                // Traditional package management - Version should be in PackageReference
                if (!packageReference.TryGetProperty("Version", out var versionElement) || versionElement.GetString() is null)
                {
                    throw new ProjectUpdaterException(UpdateCommandStrings.PackageReferenceNoVersion);
                }
                
                var packageVersion = versionElement.GetString()!;
                await AnalyzePackageForTraditionalManagementAsync(packageId, packageVersion, projectFile, context, cancellationToken);
            }
        }
    }
    }

    private static bool IsUpdatablePackage(string packageId)
    {
        return packageId.StartsWith("Aspire.")
            || packageId.StartsWith("Microsoft.Extensions.ServiceDiscovery.")
            || packageId.Equals("Microsoft.Extensions.ServiceDiscovery");
    }

    private static CentralPackageManagementInfo DetectCentralPackageManagement(FileInfo projectFile)
    {
        // Look for Directory.Packages.props in directory tree.
        for (var current = projectFile.Directory; current is not null; current = current.Parent)
        {
            var directoryPackagesPropsPath = Path.Combine(current.FullName, "Directory.Packages.props");
            if (File.Exists(directoryPackagesPropsPath))
            {
                return new CentralPackageManagementInfo(true, new FileInfo(directoryPackagesPropsPath));
            }
        }

        return new CentralPackageManagementInfo(false, null);
    }

    private async Task AnalyzePackageForTraditionalManagementAsync(string packageId, string packageVersion, FileInfo projectFile, UpdateContext context, CancellationToken cancellationToken)
    {
        var latestPackage = await GetLatestVersionOfPackageAsync(context, packageId, cancellationToken);

        if (packageVersion == latestPackage?.Version)
        {
            logger.LogInformation("Package '{PackageId}' is up to date.", packageId);
            return;
        }

        var updateStep = new PackageUpdateStep(
            string.Format(System.Globalization.CultureInfo.InvariantCulture, UpdateCommandStrings.UpdatePackageFormat, packageId, packageVersion, latestPackage!.Version),
            () => UpdatePackageReferenceInProject(projectFile, latestPackage, cancellationToken),
            packageId,
            packageVersion,
            latestPackage!.Version,
            projectFile);
        context.UpdateSteps.Enqueue(updateStep);
    }

    private async Task AnalyzePackageForCentralPackageManagementAsync(string packageId, FileInfo projectFile, FileInfo directoryPackagesPropsFile, UpdateContext context, CancellationToken cancellationToken)
    {
        var currentVersion = await GetPackageVersionFromDirectoryPackagesPropsAsync(packageId, directoryPackagesPropsFile, projectFile, cancellationToken);
        
        if (currentVersion is null)
        {
            logger.LogInformation("Package '{PackageId}' not found in Directory.Packages.props, skipping.", packageId);
            return;
        }

        var latestPackage = await GetLatestVersionOfPackageAsync(context, packageId, cancellationToken);

        if (currentVersion == latestPackage?.Version)
        {
            logger.LogInformation("Package '{PackageId}' is up to date.", packageId);
            return;
        }

        var updateStep = new PackageUpdateStep(
            string.Format(System.Globalization.CultureInfo.InvariantCulture, UpdateCommandStrings.UpdatePackageFormat, packageId, currentVersion, latestPackage!.Version),
            () => UpdatePackageVersionInDirectoryPackagesProps(packageId, latestPackage!.Version, directoryPackagesPropsFile),
            packageId,
            currentVersion,
            latestPackage!.Version,
            projectFile);
        context.UpdateSteps.Enqueue(updateStep);
    }

    private async Task<string?> GetPackageVersionFromDirectoryPackagesPropsAsync(string packageId, FileInfo directoryPackagesPropsFile, FileInfo projectFile, CancellationToken cancellationToken)
    {
        try
        {
            var doc = new XmlDocument { PreserveWhitespace = true };
            doc.Load(directoryPackagesPropsFile.FullName);
            var packageVersionNode = doc.SelectSingleNode($"/Project/ItemGroup/PackageVersion[@Include='{packageId}']");
            var versionAttribute = packageVersionNode?.Attributes?["Version"]?.Value;
            
            if (versionAttribute is null)
            {
                return null;
            }

            // Check if this is an MSBuild property expression like $(AspireVersion)
            if (IsMSBuildPropertyExpression(versionAttribute))
            {
                var propertyName = ExtractPropertyNameFromExpression(versionAttribute);
                if (propertyName is not null)
                {
                    var resolvedValue = await ResolveMSBuildPropertyAsync(propertyName, projectFile, cancellationToken);
                    if (resolvedValue is not null && IsValidSemanticVersion(resolvedValue))
                    {
                        return resolvedValue;
                    }
                    else
                    {
                        throw new ProjectUpdaterException(string.Format(System.Globalization.CultureInfo.InvariantCulture, 
                            "Unable to resolve MSBuild property '{0}' to a valid semantic version. Expression: '{1}', Resolved value: '{2}'",
                            propertyName, versionAttribute, resolvedValue ?? "null"));
                    }
                }
                else
                {
                    throw new ProjectUpdaterException(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "Invalid MSBuild property expression in package version: '{0}'", versionAttribute));
                }
            }

            return versionAttribute;
        }
        catch (ProjectUpdaterException)
        {
            // Re-throw our custom exceptions
            throw;
        }
        catch (Exception ex)
        {
            // Ignore parse errors.
            logger.LogInformation(ex, "Ignoring parsing error in Directory.Packages.props '{DirectoryPackagesPropsFile}' for project '{ProjectFile}'", directoryPackagesPropsFile.FullName, projectFile.FullName);
            return null;
        }
    }

    private static bool IsMSBuildPropertyExpression(string value)
    {
        return value.StartsWith("$(") && value.EndsWith(")") && value.Length > 3;
    }

    private static string? ExtractPropertyNameFromExpression(string expression)
    {
        if (!IsMSBuildPropertyExpression(expression))
        {
            return null;
        }

        // Extract property name from $(PropertyName)
        return expression.Substring(2, expression.Length - 3);
    }

    private async Task<string?> ResolveMSBuildPropertyAsync(string propertyName, FileInfo projectFile, CancellationToken cancellationToken)
    {
        try
        {
            var document = await GetItemsAndPropertiesAsync(
                projectFile, 
                Array.Empty<string>(), // No items needed
                [propertyName], // Just the property we want
                cancellationToken);

            var propertiesElement = document.RootElement.GetProperty("Properties");
            if (propertiesElement.TryGetProperty(propertyName, out var propertyElement))
            {
                return propertyElement.GetString();
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Exception while resolving MSBuild property '{PropertyName}' for project '{ProjectFile}'", propertyName, projectFile.FullName);
            return null;
        }
    }

    private static bool IsValidSemanticVersion(string version)
    {
        try
        {
            SemVersion.Parse(version);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task UpdatePackageVersionInDirectoryPackagesProps(string packageId, string newVersion, FileInfo directoryPackagesPropsFile)
    {
        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.Load(directoryPackagesPropsFile.FullName);
        
        var packageVersionNode = doc.SelectSingleNode($"/Project/ItemGroup/PackageVersion[@Include='{packageId}']");
        if (packageVersionNode?.Attributes?["Version"] is null)
        {
            throw new ProjectUpdaterException(string.Format(System.Globalization.CultureInfo.InvariantCulture, UpdateCommandStrings.CouldNotFindPackageVersionInDirectoryPackagesProps, packageId, directoryPackagesPropsFile.FullName));
        }

        packageVersionNode.Attributes["Version"]!.Value = newVersion;
        doc.Save(directoryPackagesPropsFile.FullName);

        await Task.CompletedTask;
    }

    private async Task UpdatePackageReferenceInProject(FileInfo projectFile, NuGetPackageCli package, CancellationToken cancellationToken)
    {
        var exitCode = await runner.AddPackageAsync(
            projectFilePath: projectFile,
            packageName: package.Id,
            packageVersion: package.Version,
            nugetSource: null, // When source is null we append --no-restore.
            options: new(),
            cancellationToken: cancellationToken);

        if (exitCode != 0)
        {
            throw new ProjectUpdaterException(string.Format(System.Globalization.CultureInfo.InvariantCulture, UpdateCommandStrings.FailedUpdatePackageReferenceFormat, package.Id, projectFile.FullName));
        }
    }

    private async Task<bool> AnalyzeAndConfirmNuGetConfigChanges(FileInfo targetFile, XmlDocument? originalDocument, XmlDocument proposedDocument, CancellationToken cancellationToken)
    {
        interactionService.DisplayEmptyLine();

        var changes = AnalyzeNuGetConfigChanges(originalDocument, proposedDocument);
        
        if (!changes.HasChanges)
        {
            interactionService.DisplayPlainText(UpdateCommandStrings.NoChangesDetectedInNuGetConfig);
            return true;
        }

        DisplayNuGetConfigChanges(changes);
        
        var shouldProceed = await interactionService.ConfirmAsync(
            UpdateCommandStrings.ApplyChangesToNuGetConfig,
            defaultValue: true,
            cancellationToken);

        return shouldProceed;
    }

    private static NuGetConfigChanges AnalyzeNuGetConfigChanges(XmlDocument? originalDocument, XmlDocument proposedDocument)
    {
        var changes = new NuGetConfigChanges();

        // Extract package sources from both documents
        var originalSources = ExtractPackageSources(originalDocument);
        var proposedSources = ExtractPackageSources(proposedDocument);

        // Analyze feed changes
        changes.AddedFeeds = proposedSources.Where(p => !originalSources.Any(o => o.Key == p.Key)).ToList();
        changes.RemovedFeeds = originalSources.Where(o => !proposedSources.Any(p => p.Key == o.Key)).ToList();
        changes.RetainedFeeds = originalSources.Where(o => proposedSources.Any(p => p.Key == o.Key)).ToList();

        // Extract package source mappings from both documents
        var originalMappings = ExtractPackageSourceMappings(originalDocument);
        var proposedMappings = ExtractPackageSourceMappings(proposedDocument);

        // Store mappings for display
        changes.OriginalMappings = originalMappings;
        changes.ProposedMappings = proposedMappings;

        // Analyze mapping changes
        changes.MappingChanges = AnalyzeMappingChanges(originalMappings, proposedMappings);

        return changes;
    }

    private static List<PackageSourceInfo> ExtractPackageSources(XmlDocument? document)
    {
        var sources = new List<PackageSourceInfo>();
        if (document?.DocumentElement == null) 
        {
            return sources;
        }

        var packageSources = document.DocumentElement.SelectSingleNode("packageSources");
        if (packageSources != null)
        {
            var addNodes = packageSources.SelectNodes("add");
            if (addNodes != null)
            {
                foreach (XmlNode addNode in addNodes)
                {
                    var key = addNode.Attributes?["key"]?.Value;
                    var value = addNode.Attributes?["value"]?.Value;
                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                    {
                        sources.Add(new PackageSourceInfo(key, value));
                    }
                }
            }
        }

        return sources;
    }

    private static Dictionary<string, List<string>> ExtractPackageSourceMappings(XmlDocument? document)
    {
        var mappings = new Dictionary<string, List<string>>();
        if (document?.DocumentElement == null) 
        {
            return mappings;
        }

        var packageSourceMapping = document.DocumentElement.SelectSingleNode("packageSourceMapping");
        if (packageSourceMapping != null)
        {
            var packageSourceNodes = packageSourceMapping.SelectNodes("packageSource");
            if (packageSourceNodes != null)
            {
                foreach (XmlNode packageSourceNode in packageSourceNodes)
                {
                    var sourceKey = packageSourceNode.Attributes?["key"]?.Value;
                    if (!string.IsNullOrEmpty(sourceKey))
                    {
                        var patterns = new List<string>();
                        var packageNodes = packageSourceNode.SelectNodes("package");
                        if (packageNodes != null)
                        {
                            foreach (XmlNode packageNode in packageNodes)
                            {
                                var pattern = packageNode.Attributes?["pattern"]?.Value;
                                if (!string.IsNullOrEmpty(pattern))
                                {
                                    patterns.Add(pattern);
                                }
                            }
                        }
                        mappings[sourceKey] = patterns;
                    }
                }
            }
        }

        return mappings;
    }

    private static List<MappingChange> AnalyzeMappingChanges(Dictionary<string, List<string>> originalMappings, Dictionary<string, List<string>> proposedMappings)
    {
        var changes = new List<MappingChange>();

        // Find sources with mapping changes
        var allSources = originalMappings.Keys.Union(proposedMappings.Keys).ToHashSet();

        foreach (var source in allSources)
        {
            var originalPatterns = originalMappings.GetValueOrDefault(source, []);
            var proposedPatterns = proposedMappings.GetValueOrDefault(source, []);

            var addedPatterns = proposedPatterns.Except(originalPatterns).ToList();
            var removedPatterns = originalPatterns.Except(proposedPatterns).ToList();

            if (addedPatterns.Count > 0 || removedPatterns.Count > 0)
            {
                changes.Add(new MappingChange(source, addedPatterns, removedPatterns));
            }
        }

        return changes;
    }

    private void DisplayNuGetConfigChanges(NuGetConfigChanges changes)
    {
        // Create a lookup of mapping changes by source for quick access
        var mappingChangesBySource = changes.MappingChanges.ToDictionary(mc => mc.SourceKey, mc => mc);

        // Display added feeds with their mappings
        foreach (var feed in changes.AddedFeeds)
        {
            interactionService.DisplayPlainText(string.Format(CultureInfo.InvariantCulture, UpdateCommandStrings.AddedFeedFormat, feed.Value));
            interactionService.DisplayEmptyLine();
            
            if (changes.ProposedMappings.TryGetValue(feed.Key, out var patterns))
            {
                foreach (var pattern in patterns)
                {
                    interactionService.DisplayPlainText(string.Format(CultureInfo.InvariantCulture, UpdateCommandStrings.MappingAddedFormat, pattern));
                }
            }
            interactionService.DisplayEmptyLine();
        }

        // Display removed feeds
        foreach (var feed in changes.RemovedFeeds)
        {
            interactionService.DisplayPlainText(string.Format(CultureInfo.InvariantCulture, UpdateCommandStrings.RemovedFeedFormat, feed.Value));
            interactionService.DisplayEmptyLine();
        }

        // Display retained feeds with their mapping changes and current mappings
        foreach (var feed in changes.RetainedFeeds)
        {
            interactionService.DisplayPlainText(string.Format(CultureInfo.InvariantCulture, UpdateCommandStrings.RetainedFeedFormat, feed.Value));
            interactionService.DisplayEmptyLine();
            
            if (mappingChangesBySource.TryGetValue(feed.Key, out var mappingChange))
            {
                // Show added patterns
                foreach (var pattern in mappingChange.AddedPatterns)
                {
                    interactionService.DisplayPlainText(string.Format(CultureInfo.InvariantCulture, UpdateCommandStrings.MappingAddedFormat, pattern));
                }
                
                // Show removed patterns
                foreach (var pattern in mappingChange.RemovedPatterns)
                {
                    interactionService.DisplayPlainText(string.Format(CultureInfo.InvariantCulture, UpdateCommandStrings.MappingRemovedFormat, pattern));
                }
            }
            
            // Show current/unchanged mappings in the proposed configuration
            if (changes.ProposedMappings.TryGetValue(feed.Key, out var currentPatterns))
            {
                var addedPatterns = mappingChangesBySource.TryGetValue(feed.Key, out var currentMappingChange) ? currentMappingChange.AddedPatterns : new List<string>();
                
                foreach (var pattern in currentPatterns)
                {
                    // Only show patterns that weren't added (they are existing/unchanged)
                    if (!addedPatterns.Contains(pattern))
                    {
                        interactionService.DisplayPlainText(string.Format(CultureInfo.InvariantCulture, UpdateCommandStrings.MappingRetainedFormat, pattern));
                    }
                }
            }
            interactionService.DisplayEmptyLine();
        }
    }
}

internal record PackageSourceInfo(string Key, string Value);

internal record MappingChange(string SourceKey, List<string> AddedPatterns, List<string> RemovedPatterns);

internal class NuGetConfigChanges
{
    public List<PackageSourceInfo> AddedFeeds { get; set; } = [];
    public List<PackageSourceInfo> RemovedFeeds { get; set; } = [];
    public List<PackageSourceInfo> RetainedFeeds { get; set; } = [];
    public List<MappingChange> MappingChanges { get; set; } = [];
    public Dictionary<string, List<string>> OriginalMappings { get; set; } = new();
    public Dictionary<string, List<string>> ProposedMappings { get; set; } = new();

    public bool HasChanges => AddedFeeds.Count > 0 || RemovedFeeds.Count > 0 || MappingChanges.Count > 0;
}

internal sealed class ProjectUpdateResult
{
    public bool UpdatedApplied { get; set; }
}

internal sealed class UpdateContext(FileInfo appHostProjectFile, PackageChannel channel)
{
    public FileInfo AppHostProjectFile { get; } = appHostProjectFile;
    public PackageChannel Channel { get; } = channel;
    public ConcurrentQueue<UpdateStep> UpdateSteps { get; } = new();
    public ConcurrentQueue<AnalyzeStep> AnalyzeSteps { get; } = new();
    public HashSet<string> VisitedProjects { get; } = new();
    public bool FallbackXmlParsing { get; set; }
}

internal abstract record UpdateStep(string Description, Func<Task> Callback)
{
    /// <summary>
    /// Gets the formatted display text using Spectre Console markup for enhanced visual presentation.
    /// </summary>
    public virtual string GetFormattedDisplayText() => Description;
}

/// <summary>
/// Represents an update step for a package reference, containing package and project information.
/// </summary>
internal record PackageUpdateStep(
    string Description, 
    Func<Task> Callback,
    string PackageId,
    string CurrentVersion,
    string NewVersion,
    FileInfo ProjectFile) : UpdateStep(Description, Callback)
{
    public override string GetFormattedDisplayText()
    {
        return $"[bold yellow]{PackageId}[/] [bold green]{CurrentVersion}[/] to [bold green]{NewVersion}[/]";
    }
}

internal record AnalyzeStep(string Description, Func<Task> Callback);

internal sealed class ProjectUpdaterException : System.Exception
{
    public ProjectUpdaterException(string message) : base(message) { }
    public ProjectUpdaterException(string message, System.Exception inner) : base(message, inner) { }
}

internal record CentralPackageManagementInfo(bool UsesCentralPackageManagement, FileInfo? DirectoryPackagesPropsFile);