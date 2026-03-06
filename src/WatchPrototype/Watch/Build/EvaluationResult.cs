// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Build.Execution;
using Microsoft.Build.Graph;
using Microsoft.DotNet.HotReload;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

internal sealed class EvaluationResult(
    LoadedProjectGraph projectGraph,
    IReadOnlyDictionary<ProjectInstanceId, ProjectInstance> restoredProjectInstances,
    IReadOnlyDictionary<string, FileItem> files,
    IReadOnlyDictionary<ProjectInstanceId, StaticWebAssetsManifest> staticWebAssetsManifests,
    ProjectBuildManager buildManager)
{
    public readonly IReadOnlyDictionary<string, FileItem> Files = files;
    public readonly LoadedProjectGraph ProjectGraph = projectGraph;
    public readonly ProjectBuildManager BuildManager = buildManager;

    public readonly FilePathExclusions ItemExclusions
        = projectGraph != null ? FilePathExclusions.Create(projectGraph.Graph) : FilePathExclusions.Empty;

    private readonly Lazy<IReadOnlySet<string>> _lazyBuildFiles
        = new(() => projectGraph != null ? CreateBuildFileSet(projectGraph.Graph) : new HashSet<string>());

    private static IReadOnlySet<string> CreateBuildFileSet(ProjectGraph projectGraph)
        => projectGraph.ProjectNodes.SelectMany(p => p.ProjectInstance.ImportPaths)
            .Concat(projectGraph.ProjectNodes.Select(p => p.ProjectInstance.FullPath))
            .ToHashSet(PathUtilities.OSSpecificPathComparer);

    public IReadOnlySet<string> BuildFiles
        => _lazyBuildFiles.Value;

    public IReadOnlyDictionary<ProjectInstanceId, StaticWebAssetsManifest> StaticWebAssetsManifests
        => staticWebAssetsManifests;

    public IReadOnlyDictionary<ProjectInstanceId, ProjectInstance> RestoredProjectInstances
        => restoredProjectInstances;

    public void WatchFiles(FileWatcher fileWatcher)
    {
        fileWatcher.WatchContainingDirectories(Files.Keys, includeSubdirectories: true);

        fileWatcher.WatchContainingDirectories(
            StaticWebAssetsManifests.Values.SelectMany(static manifest => manifest.DiscoveryPatterns.Select(static pattern => pattern.Directory)),
            includeSubdirectories: true);

        fileWatcher.WatchFiles(BuildFiles);
    }

    public static ImmutableDictionary<string, string> GetGlobalBuildProperties(IEnumerable<string> buildArguments, EnvironmentOptions environmentOptions)
    {
        // See https://github.com/dotnet/project-system/blob/main/docs/well-known-project-properties.md

        return BuildUtilities.ParseBuildProperties(buildArguments)
            .ToImmutableDictionary(keySelector: arg => arg.key, elementSelector: arg => arg.value)
            .SetItem(PropertyNames.DotNetWatchBuild, "true")
            .SetItem(PropertyNames.DesignTimeBuild, "true")
            .SetItem(PropertyNames.SkipCompilerExecution, "true")
            .SetItem(PropertyNames.ProvideCommandLineArgs, "true")
            // this will force CoreCompile task to execute and return command line args even if all inputs and outputs are up to date:
            .SetItem(PropertyNames.NonExistentFile, "__NonExistentSubDir__\\__NonExistentFile__");
    }

    /// <summary>
    /// Loads project graph and performs design-time build.
    /// </summary>
    public static async ValueTask<EvaluationResult?> TryCreateAsync(
        ProjectGraphFactory factory,
        GlobalOptions globalOptions,
        EnvironmentOptions environmentOptions,
        bool restore,
        CancellationToken cancellationToken)
    {
        var logger = factory.Logger;
        var stopwatch = Stopwatch.StartNew();

        var projectGraph = factory.TryLoadProjectGraph(projectGraphRequired: true, cancellationToken);

        if (projectGraph == null)
        {
            return null;
        }

        var buildReporter = new BuildReporter(projectGraph.Logger, globalOptions, environmentOptions);
        var buildManager = new ProjectBuildManager(projectGraph.ProjectCollection, buildReporter);

        logger.LogDebug("Project graph loaded in {Time}s.", stopwatch.Elapsed.TotalSeconds.ToString("0.0"));

        if (restore)
        {
            stopwatch.Restart();

            var restoreRequests = projectGraph.Graph.GraphRoots.Select(node => BuildRequest.Create(node.ProjectInstance, [TargetNames.Restore])).ToArray();

            if (await buildManager.BuildAsync(
                restoreRequests,
                onFailure: failedInstance =>
                {
                    logger.LogError("Failed to restore project '{Path}'.", failedInstance.FullPath);

                    // terminate build on first failure:
                    return false;
                },
                operationName: "Restore",
                cancellationToken) is [])
            {
                return null;
            }

            logger.LogDebug("Projects restored in {Time}s.", stopwatch.Elapsed.TotalSeconds.ToString("0.0"));
        }

        stopwatch.Restart();

        // Capture the snapshot of original project instances after Restore target has been run.
        // These instances can be used to evaluate additional targets (e.g. deployment) if needed.
        var restoredProjectInstances = projectGraph.Graph.ProjectNodes.ToDictionary(
            keySelector: node => node.ProjectInstance.GetId(),
            elementSelector: node => node.ProjectInstance.DeepCopy());

        // Update the project instances of the graph with design-time build results.
        // The properties and items set by DTB will be used by the Workspace to create Roslyn representation of projects.

        var buildRequests =
           (from node in projectGraph.Graph.ProjectNodesTopologicallySorted
            where node.ProjectInstance.GetPropertyValue(PropertyNames.TargetFramework) != ""
            let targets = GetBuildTargets(node.ProjectInstance, environmentOptions)
            where targets is not []
            select BuildRequest.Create(node.ProjectInstance, [.. targets])).ToArray();

        var buildResults = await buildManager.BuildAsync(
            buildRequests,
            onFailure: failedInstance =>
            {
                logger.LogError("Failed to build project '{Path}'.", failedInstance.FullPath);

                // terminate build on first failure:
                return false;
            },
            operationName: "DesignTimeBuild",
            cancellationToken);

        if (buildResults is [])
        {
            return null;
        }

        logger.LogDebug("Design-time build completed in {Time}s.", stopwatch.Elapsed.TotalSeconds.ToString("0.0"));

        ProcessBuildResults(buildResults, logger, out var fileItems, out var staticWebAssetManifests);

        BuildReporter.ReportWatchedFiles(logger, fileItems);

        return new EvaluationResult(projectGraph, restoredProjectInstances, fileItems, staticWebAssetManifests, buildManager);
    }

    private static void ProcessBuildResults(
        ImmutableArray<BuildResult<object?>> buildResults,
        ILogger logger,
        out IReadOnlyDictionary<string, FileItem> fileItems,
        out IReadOnlyDictionary<ProjectInstanceId, StaticWebAssetsManifest> staticWebAssetManifests)
    {
        var fileItemsBuilder = new Dictionary<string, FileItem>();
        var staticWebAssetManifestsBuilder = new Dictionary<ProjectInstanceId, StaticWebAssetsManifest>();

        foreach (var buildResult in buildResults)
        {
            Debug.Assert(buildResult.IsSuccess);

            var projectInstance = buildResult.ProjectInstance;
            Debug.Assert(projectInstance != null);

            // command line args items should be available:
            Debug.Assert(
                !Path.GetExtension(projectInstance.FullPath).Equals(".csproj", PathUtilities.OSSpecificPathComparison) ||
                projectInstance.GetItems("CscCommandLineArgs").Any());

            var projectPath = projectInstance.FullPath;
            var projectDirectory = Path.GetDirectoryName(projectPath)!;

            if (buildResult.TargetResults.ContainsKey(TargetNames.GenerateComputedBuildStaticWebAssets) &&
                projectInstance.GetIntermediateOutputDirectory() is { } outputDir &&
                StaticWebAssetsManifest.TryParseFile(Path.Combine(outputDir, StaticWebAsset.ManifestFileName), logger) is { } manifest)
            {
                staticWebAssetManifestsBuilder.Add(projectInstance.GetId(), manifest);

                // watch asset files, but not bundle files as they are regenarated when scoped CSS files are updated:
                foreach (var (relativeUrl, filePath) in manifest.UrlToPathMap)
                {
                    if (!StaticWebAsset.IsCompressedAssetFile(filePath) && !StaticWebAsset.IsScopedCssBundleFile(filePath))
                    {
                        AddFile(filePath, staticWebAssetRelativeUrl: relativeUrl);
                    }
                }
            }

            // Adds file items for scoped css files.
            // Scoped css files are bundled into a single entry per project that is represented in the static web assets manifest,
            // but we need to watch the original individual files.
            if (buildResult.TargetResults.ContainsKey(TargetNames.ResolveScopedCssInputs))
            {
                foreach (var item in projectInstance.GetItems(ItemNames.ScopedCssInput))
                {
                    AddFile(item.EvaluatedInclude, staticWebAssetRelativeUrl: null);
                }
            }

            // Add Watch items after other items so that we don't override properties set above.
            var items = projectInstance.GetItems(ItemNames.Compile)
                .Concat(projectInstance.GetItems(ItemNames.AdditionalFiles))
                .Concat(projectInstance.GetItems(ItemNames.Watch));

            foreach (var item in items)
            {
                AddFile(item.EvaluatedInclude, staticWebAssetRelativeUrl: null);
            }

            void AddFile(string relativePath, string? staticWebAssetRelativeUrl)
            {
                var filePath = Path.GetFullPath(Path.Combine(projectDirectory, relativePath));

                if (!fileItemsBuilder.TryGetValue(filePath, out var existingFile))
                {
                    fileItemsBuilder.Add(filePath, new FileItem
                    {
                        FilePath = filePath,
                        ContainingProjectPaths = [projectPath],
                        StaticWebAssetRelativeUrl = staticWebAssetRelativeUrl,
                    });
                }
                else if (!existingFile.ContainingProjectPaths.Contains(projectPath))
                {
                    // linked files might be included to multiple projects:
                    existingFile.ContainingProjectPaths.Add(projectPath);
                }
            }
        }

        fileItems = fileItemsBuilder;
        staticWebAssetManifests = staticWebAssetManifestsBuilder;
    }

    private static string[] GetBuildTargets(ProjectInstance projectInstance, EnvironmentOptions environmentOptions)
    {
        var compileTarget = projectInstance.Targets.ContainsKey(TargetNames.CompileDesignTime)
            ? TargetNames.CompileDesignTime
            : projectInstance.Targets.ContainsKey(TargetNames.Compile)
            ? TargetNames.Compile
            : null;

        if (compileTarget == null)
        {
            return [];
        }

        var targets = new List<string>
        {
            compileTarget
        };

        if (!environmentOptions.SuppressHandlingStaticWebAssets)
        {
            // generates static file asset manifest
            if (projectInstance.Targets.ContainsKey(TargetNames.GenerateComputedBuildStaticWebAssets))
            {
                targets.Add(TargetNames.GenerateComputedBuildStaticWebAssets);
            }

            // populates ScopedCssInput items:
            if (projectInstance.Targets.ContainsKey(TargetNames.ResolveScopedCssInputs))
            {
                targets.Add(TargetNames.ResolveScopedCssInputs);
            }
        }

        targets.AddRange(projectInstance.GetStringListPropertyValue(PropertyNames.CustomCollectWatchItems));
        return [.. targets];
    }
}
