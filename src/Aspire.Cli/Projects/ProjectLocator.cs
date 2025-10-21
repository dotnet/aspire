// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Projects;

internal interface IProjectLocator
{
    Task<AppHostProjectSearchResult> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, MultipleAppHostProjectsFoundBehavior multipleAppHostProjectsFoundBehavior, bool createSettingsFile, CancellationToken cancellationToken = default);
    Task<FileInfo?> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, bool createSettingsFile, CancellationToken cancellationToken);
}

internal sealed class ProjectLocator(ILogger<ProjectLocator> logger, IDotNetCliRunner runner, CliExecutionContext executionContext, IInteractionService interactionService, IConfigurationService configurationService, AspireCliTelemetry telemetry, IFeatures features) : IProjectLocator
{
    private static readonly HashSet<string> s_supportedProjectFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".csproj", ".fsproj", ".vbproj" };

    public async Task<List<FileInfo>> FindAppHostProjectFilesAsync(string searchDirectory, CancellationToken cancellationToken)
    {
        var allCandidates = await FindAppHostProjectFilesAsync(new DirectoryInfo(searchDirectory), cancellationToken);
        return [..allCandidates.BuildableAppHost, ..allCandidates.UnbuildableSuspectedAppHostProjects];
    }

    private async Task<(List<FileInfo> BuildableAppHost, List<FileInfo> UnbuildableSuspectedAppHostProjects)> FindAppHostProjectFilesAsync(DirectoryInfo searchDirectory, CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();

        return await interactionService.ShowStatusAsync(InteractionServiceStrings.SearchingProjects, async () =>
        {
            var appHostProjects = new List<FileInfo>();
            var unbuildableSuspectedAppHostProjects = new List<FileInfo>();
            var lockObject = new object();
            logger.LogDebug("Searching for project files in {SearchDirectory}", searchDirectory.FullName);
            var enumerationOptions = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true
            };

            interactionService.DisplayMessage("magnifying_glass_tilted_left", InteractionServiceStrings.FindingAppHosts);

            // Scan for *.csproj files (existing logic)
            var projectFiles = searchDirectory.GetFiles("*.csproj", enumerationOptions);
            logger.LogDebug("Found {ProjectFileCount} project files in {SearchDirectory}", projectFiles.Length, searchDirectory.FullName);

            var parallelOptions = new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            await Parallel.ForEachAsync(projectFiles, parallelOptions, async (projectFile, ct) =>
            {
                logger.LogDebug("Checking project file {ProjectFile}", projectFile.FullName);
                var information = await runner.GetAppHostInformationAsync(projectFile, new DotNetCliRunnerInvocationOptions(), ct);

                if (information.ExitCode == 0 && information.IsAspireHost)
                {
                    logger.LogDebug("Found AppHost project file {ProjectFile} in {SearchDirectory}", projectFile.FullName, searchDirectory.FullName);
                    var relativePath = Path.GetRelativePath(executionContext.WorkingDirectory.FullName, projectFile.FullName);
                    interactionService.DisplaySubtleMessage(relativePath);
                    lock (lockObject)
                    {
                        appHostProjects.Add(projectFile);
                    }
                }
                else if (IsPossiblyUnbuildableAppHost(projectFile))
                {
                    var relativePath = Path.GetRelativePath(executionContext.WorkingDirectory.FullName, projectFile.FullName);
                    interactionService.DisplayMessage("warning", string.Format(CultureInfo.CurrentCulture, ErrorStrings.ProjectFileMayBeUnbuildableAppHost, relativePath));
                    unbuildableSuspectedAppHostProjects.Add(projectFile);
                }
                else
                {
                    logger.LogTrace("Project file {ProjectFile} in {SearchDirectory} is not an Aspire host", projectFile.FullName, searchDirectory.FullName);
                }
            });

            // Scan for single-file apphosts (new logic)
            if (features.IsFeatureEnabled(KnownFeatures.SingleFileAppHostEnabled, false))
            {
                logger.LogDebug("Searching for single-file apphosts in {SearchDirectory}", searchDirectory.FullName);
                var candidateAppHostFiles = searchDirectory.GetFiles("apphost.cs", enumerationOptions);
                logger.LogDebug("Found {CandidateFileCount} single-file apphost candidates in {SearchDirectory}", candidateAppHostFiles.Length, searchDirectory.FullName);

                await Parallel.ForEachAsync(candidateAppHostFiles, parallelOptions, async (candidateFile, ct) =>
                {
                    logger.LogDebug("Checking single-file apphost candidate {CandidateFile}", candidateFile.FullName);

                    if (await IsValidSingleFileAppHostAsync(candidateFile, ct))
                    {
                        logger.LogDebug("Found single-file apphost candidate {CandidateFile} in {SearchDirectory}", candidateFile.FullName, searchDirectory.FullName);
                        var relativePath = Path.GetRelativePath(executionContext.WorkingDirectory.FullName, candidateFile.FullName);
                        interactionService.DisplaySubtleMessage(relativePath);
                        lock (lockObject)
                        {
                            appHostProjects.Add(candidateFile);
                        }
                    }
                    else
                    {
                        logger.LogTrace("Single-file candidate {CandidateFile} in {SearchDirectory} is not a valid apphost", candidateFile.FullName, searchDirectory.FullName);
                    }
                });
            }
            else
            {
                logger.LogTrace("Single-file apphost feature is disabled, skipping single-file apphost discovery");
            }

            // This sort is done here to make results deterministic since we get all the app
            // host information in parallel and the order may vary.
            appHostProjects.Sort((x, y) => x.FullName.CompareTo(y.FullName));

            return (appHostProjects, unbuildableSuspectedAppHostProjects);
        });
    }

    private static bool IsPossiblyUnbuildableAppHost(FileInfo projectFile)
    {
        var fileNameSuggestsAppHost = () => projectFile.Name.EndsWith("AppHost.csproj", StringComparison.OrdinalIgnoreCase);
        var folderContainsAppHostCSharpFile = () => projectFile.Directory!.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Any(f => f.Name.Equals("AppHost.cs", StringComparison.OrdinalIgnoreCase));
        return fileNameSuggestsAppHost() || folderContainsAppHostCSharpFile();
    }

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
        catch
        {
            // If we can't read the file, it's not a valid candidate
            return false;
        }

        return false;
    }

    private async Task<FileInfo?> GetAppHostProjectFileFromSettingsAsync(CancellationToken cancellationToken)
    {
        var searchDirectory = executionContext.WorkingDirectory;

        while (true)
        {
            var settingsFile = new FileInfo(ConfigurationHelper.BuildPathToSettingsJsonFile(searchDirectory.FullName));

            if (settingsFile.Exists)
            {
                using var stream = settingsFile.OpenRead();
                var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                if (json.RootElement.TryGetProperty("appHostPath", out var appHostPathProperty) && appHostPathProperty.GetString() is { } appHostPath)
                {

                    var qualifiedAppHostPath = Path.IsPathRooted(appHostPath) ? appHostPath : Path.Combine(settingsFile.Directory!.FullName, appHostPath);
                    var appHostFile = new FileInfo(qualifiedAppHostPath);

                    if (appHostFile.Exists)
                    {
                        return appHostFile;
                    }
                    else
                    {
                        // AppHost file was specified but doesn't exist, return null to trigger fallback logic
                        interactionService.DisplayMessage("warning", string.Format(CultureInfo.CurrentCulture, ErrorStrings.AppHostWasSpecifiedButDoesntExist, settingsFile.FullName, qualifiedAppHostPath));
                        return null;
                    }
                }
            }

            if (searchDirectory.Parent is not null)
            {
                searchDirectory = searchDirectory.Parent;
            }
            else
            {
                return null;
            }
        }
    }

    public async Task<AppHostProjectSearchResult> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, MultipleAppHostProjectsFoundBehavior multipleAppHostProjectsFoundBehavior, bool createSettingsFile, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Finding project file in {CurrentDirectory}", executionContext.WorkingDirectory);

        if (projectFile is not null)
        {
            // Check if the provided path is actually a directory
            if (Directory.Exists(projectFile.FullName))
            {
                logger.LogDebug("Provided path {Path} is a directory, searching for project files recursively", projectFile.FullName);
                var directory = new DirectoryInfo(projectFile.FullName);

                // Search recursively for .csproj files and validate they are AppHost projects
                // Use ShowStatusAsync and parallel processing similar to FindAppHostProjectFilesAsync
                var appHostProjects = await interactionService.ShowStatusAsync(InteractionServiceStrings.SearchingProjects, async () =>
                {
                    var enumerationOptions = new EnumerationOptions
                    {
                        RecurseSubdirectories = true,
                        IgnoreInaccessible = true
                    };

                    interactionService.DisplayMessage("magnifying_glass_tilted_left", InteractionServiceStrings.FindingAppHosts);

                    var allProjectFiles = directory.GetFiles("*.csproj", enumerationOptions);
                    logger.LogDebug("Found {ProjectFileCount} project files in {Directory}", allProjectFiles.Length, directory.FullName);

                    var foundProjects = new List<FileInfo>();
                    var lockObject = new object();

                    var parallelOptions = new ParallelOptions
                    {
                        CancellationToken = cancellationToken,
                        MaxDegreeOfParallelism = Environment.ProcessorCount
                    };

                    // Validate each project to see if it's an AppHost in parallel
                    await Parallel.ForEachAsync(allProjectFiles, parallelOptions, async (candidateProject, ct) =>
                    {
                        logger.LogDebug("Checking project file {ProjectFile}", candidateProject.FullName);
                        var information = await runner.GetAppHostInformationAsync(candidateProject, new DotNetCliRunnerInvocationOptions(), ct);

                        if (information.ExitCode == 0 && information.IsAspireHost)
                        {
                            logger.LogDebug("Found AppHost project file {ProjectFile}", candidateProject.FullName);
                            var relativePath = Path.GetRelativePath(executionContext.WorkingDirectory.FullName, candidateProject.FullName);
                            interactionService.DisplaySubtleMessage(relativePath);
                            lock (lockObject)
                            {
                                foundProjects.Add(candidateProject);
                            }
                        }
                    });

                    // If no .csproj AppHost files found and single-file apphost is enabled, check for apphost.cs
                    if (foundProjects.Count == 0 && features.IsFeatureEnabled(KnownFeatures.SingleFileAppHostEnabled, false))
                    {
                        var appHostFiles = directory.GetFiles("apphost.cs", enumerationOptions);
                        logger.LogDebug("Found {CandidateFileCount} single-file apphost candidates", appHostFiles.Length);

                        await Parallel.ForEachAsync(appHostFiles, parallelOptions, async (candidateFile, ct) =>
                        {
                            logger.LogDebug("Checking single-file apphost candidate {CandidateFile}", candidateFile.FullName);

                            if (await IsValidSingleFileAppHostAsync(candidateFile, ct))
                            {
                                logger.LogDebug("Found valid single-file apphost {AppHostFile}", candidateFile.FullName);
                                var relativePath = Path.GetRelativePath(executionContext.WorkingDirectory.FullName, candidateFile.FullName);
                                interactionService.DisplaySubtleMessage(relativePath);
                                lock (lockObject)
                                {
                                    foundProjects.Add(candidateFile);
                                }
                            }
                        });
                    }

                    // Sort for deterministic results
                    foundProjects.Sort((x, y) => x.FullName.CompareTo(y.FullName));

                    return foundProjects;
                });

                interactionService.DisplayEmptyLine();

                if (appHostProjects.Count == 0)
                {
                    logger.LogError("No AppHost project files found in directory {Directory}", directory.FullName);
                    throw new ProjectLocatorException(ErrorStrings.ProjectFileDoesntExist);
                }
                else if (appHostProjects.Count == 1)
                {
                    logger.LogDebug("Found single AppHost project file {ProjectFile} in directory {Directory}", appHostProjects[0].FullName, directory.FullName);
                    projectFile = appHostProjects[0];
                }
                else
                {
                    if (multipleAppHostProjectsFoundBehavior is MultipleAppHostProjectsFoundBehavior.Prompt)
                    {
                        logger.LogDebug("Multiple AppHost project files found in directory {Directory}, prompting user to select", directory.FullName);
                        projectFile = await interactionService.PromptForSelectionAsync(
                            InteractionServiceStrings.SelectAppHostToUse,
                            appHostProjects,
                            file => $"{file.Name} ({Path.GetRelativePath(executionContext.WorkingDirectory.FullName, file.FullName)})",
                            cancellationToken
                        );
                    }
                    else if (multipleAppHostProjectsFoundBehavior is MultipleAppHostProjectsFoundBehavior.None)
                    {
                        logger.LogDebug("Multiple AppHost project files found in directory {Directory}, selecting none", directory.FullName);
                        projectFile = null;
                    }
                    else if (multipleAppHostProjectsFoundBehavior is MultipleAppHostProjectsFoundBehavior.Throw)
                    {
                        logger.LogError("Multiple AppHost project files found in directory {Directory}, throwing exception", directory.FullName);
                        throw new ProjectLocatorException(ErrorStrings.MultipleProjectFilesFound);
                    }
                }
            }

            if (projectFile is not null)
            {
                // If the project file is passed, validate it.
                if (!projectFile.Exists)
                {
                    logger.LogError("Project file {ProjectFile} does not exist.", projectFile.FullName);
                    throw new ProjectLocatorException(ErrorStrings.ProjectFileDoesntExist);
                }

                // Handle explicit apphost.cs files
                if (projectFile.Name.Equals("apphost.cs", StringComparison.OrdinalIgnoreCase))
                {
                    if (features.IsFeatureEnabled(KnownFeatures.SingleFileAppHostEnabled, false))
                    {
                        if (await IsValidSingleFileAppHostAsync(projectFile, cancellationToken))
                        {
                            logger.LogDebug("Using single-file apphost {ProjectFile}", projectFile.FullName);
                            return new AppHostProjectSearchResult(projectFile, [projectFile]);
                        }
                        else if (projectFile.Directory is { } parentDirectory)
                        {
                            // File exists but we are not in a single file apphost. Search in the parent directory for a valid apphost csproj
                            return await UseOrFindAppHostProjectFileAsync(new FileInfo(parentDirectory.FullName), multipleAppHostProjectsFoundBehavior, createSettingsFile, cancellationToken);
                        }
                        else
                        {
                            throw new ProjectLocatorException(ErrorStrings.ProjectFileDoesntExist);
                        }
                    }
                    else
                    {
                        throw new ProjectLocatorException(ErrorStrings.ProjectFileDoesntExist);
                    }
                }
                // Handle .cs|fs|vbproj files
                else if (s_supportedProjectFileExtensions.Contains(projectFile.Extension))
                {
                    logger.LogDebug("Using project file {ProjectFile}", projectFile.FullName);
                    return new AppHostProjectSearchResult(projectFile, [projectFile]);
                }
                // Reject other extensions
                else
                {
                    throw new ProjectLocatorException(ErrorStrings.ProjectFileDoesntExist);
                }
            }
        }

        projectFile = await GetAppHostProjectFileFromSettingsAsync(cancellationToken);

        if (projectFile is not null)
        {
            return new AppHostProjectSearchResult(projectFile, [projectFile]);
        }

        logger.LogDebug("No project file specified, searching for *.csproj files in {CurrentDirectory}", executionContext.WorkingDirectory);
        var results = await FindAppHostProjectFilesAsync(executionContext.WorkingDirectory, cancellationToken);
        interactionService.DisplayEmptyLine();

        logger.LogDebug("Found {ProjectFileCount} project files.", results.BuildableAppHost.Count);

        FileInfo? selectedAppHost = null;

        if (results.BuildableAppHost.Count == 0 && results.UnbuildableSuspectedAppHostProjects.Count == 0)
        {
            throw new ProjectLocatorException(ErrorStrings.NoProjectFileFound);
        }
        else if (results.BuildableAppHost.Count == 0 && results.UnbuildableSuspectedAppHostProjects.Count > 0)
        {
            throw new ProjectLocatorException(ErrorStrings.AppHostsMayNotBeBuildable);
        }
        else if (results.BuildableAppHost.Count == 1)
        {
            selectedAppHost = results.BuildableAppHost[0];
        }
        else if (results.BuildableAppHost.Count > 1)
        {
            selectedAppHost = multipleAppHostProjectsFoundBehavior switch
            {
                MultipleAppHostProjectsFoundBehavior.Throw => throw new ProjectLocatorException(ErrorStrings.MultipleProjectFilesFound),
                MultipleAppHostProjectsFoundBehavior.Prompt => await interactionService.PromptForSelectionAsync(InteractionServiceStrings.SelectAppHostToUse, results.BuildableAppHost, projectFile => $"{projectFile.Name} ({Path.GetRelativePath(executionContext.WorkingDirectory.FullName, projectFile.FullName)})", cancellationToken),
                MultipleAppHostProjectsFoundBehavior.None => null,
                _ => selectedAppHost
            };
        }

        if (createSettingsFile)
        {
            await CreateSettingsFileAsync(selectedAppHost!, cancellationToken);
        }

        return new AppHostProjectSearchResult(selectedAppHost, results.BuildableAppHost);
    }

    public async Task<FileInfo?> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, bool createSettingsFile, CancellationToken cancellationToken = default)
    {
        var result = await UseOrFindAppHostProjectFileAsync(projectFile, MultipleAppHostProjectsFoundBehavior.Prompt, createSettingsFile, cancellationToken);
        return result.SelectedProjectFile;
    }

    private async Task CreateSettingsFileAsync(FileInfo projectFile, CancellationToken cancellationToken)
    {
        var settingsFilePath = ConfigurationHelper.BuildPathToSettingsJsonFile(executionContext.WorkingDirectory.FullName);
        var settingsFile = new FileInfo(settingsFilePath);

        logger.LogDebug("Creating settings file at {SettingsFilePath}", settingsFile.FullName);

        var relativePathToProjectFile = Path.GetRelativePath(settingsFile.Directory!.FullName, projectFile.FullName).Replace(Path.DirectorySeparatorChar, '/');

        // Use the configuration writer to set the appHostPath, which will merge with any existing settings
        await configurationService.SetConfigurationAsync("appHostPath", relativePathToProjectFile, isGlobal: false, cancellationToken);

        var relativeSettingsFilePath = Path.GetRelativePath(executionContext.WorkingDirectory.FullName, settingsFile.FullName).Replace(Path.DirectorySeparatorChar, '/');
        interactionService.DisplayMessage("file_cabinet", string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.CreatedSettingsFile, $"[bold]'{relativeSettingsFilePath}'[/]"));
    }

    public async Task<IReadOnlyList<FileInfo>> FindExecutableProjectsAsync(string searchDirectory, CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();

        return await interactionService.ShowStatusAsync(InteractionServiceStrings.SearchingProjects, async () =>
        {
            var executableProjects = new List<FileInfo>();
            var lockObject = new object();
            logger.LogDebug("Searching for executable project files in {SearchDirectory}", searchDirectory);

            var enumerationOptions = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true
            };

            var searchDir = new DirectoryInfo(searchDirectory);
            var projectFiles = searchDir.GetFiles("*.csproj", enumerationOptions);
            logger.LogDebug("Found {ProjectFileCount} project files in {SearchDirectory}", projectFiles.Length, searchDirectory);

            var parallelOptions = new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            await Parallel.ForEachAsync(projectFiles, parallelOptions, async (projectFile, ct) =>
            {
                logger.LogDebug("Checking project file {ProjectFile} for OutputType", projectFile.FullName);

                var (exitCode, jsonDocument) = await runner.GetProjectItemsAndPropertiesAsync(
                    projectFile,
                    [],
                    ["OutputType"],
                    new DotNet.DotNetCliRunnerInvocationOptions(),
                    ct);

                if (exitCode == 0 && jsonDocument != null)
                {
                    var rootElement = jsonDocument.RootElement;
                    if (rootElement.TryGetProperty("Properties", out var properties))
                    {
                        if (properties.TryGetProperty("OutputType", out var outputTypeElement))
                        {
                            var outputType = outputTypeElement.GetString();
                            if (outputType == "Exe" || outputType == "WinExe")
                            {
                                logger.LogDebug("Found executable project file {ProjectFile} with OutputType {OutputType}", projectFile.FullName, outputType);
                                lock (lockObject)
                                {
                                    executableProjects.Add(projectFile);
                                }
                            }
                        }
                    }
                }
            });

            // Sort for deterministic results
            executableProjects.Sort((x, y) => x.FullName.CompareTo(y.FullName));

            return executableProjects;
        });
    }
}

internal class ProjectLocatorException : System.Exception
{
    public ProjectLocatorException(string message) : base(message) { }
}

internal record AppHostProjectSearchResult(FileInfo? SelectedProjectFile, List<FileInfo> AllProjectFileCandidates);

internal enum MultipleAppHostProjectsFoundBehavior
{
    Prompt,
    Throw,
    None
}
