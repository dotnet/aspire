// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.Build.Execution;
using Microsoft.Build.Graph;
using Microsoft.DotNet.HotReload;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

internal sealed class EvaluationResult(ProjectGraph projectGraph, IReadOnlyDictionary<string, FileItem> files, IReadOnlyDictionary<ProjectInstanceId, StaticWebAssetsManifest> staticWebAssetsManifests)
{
    public readonly IReadOnlyDictionary<string, FileItem> Files = files;
    public readonly ProjectGraph ProjectGraph = projectGraph;

    public readonly FilePathExclusions ItemExclusions
        = projectGraph != null ? FilePathExclusions.Create(projectGraph) : FilePathExclusions.Empty;

    private readonly Lazy<IReadOnlySet<string>> _lazyBuildFiles
        = new(() => projectGraph != null ? CreateBuildFileSet(projectGraph) : new HashSet<string>());

    private static IReadOnlySet<string> CreateBuildFileSet(ProjectGraph projectGraph)
        => projectGraph.ProjectNodes.SelectMany(p => p.ProjectInstance.ImportPaths)
            .Concat(projectGraph.ProjectNodes.Select(p => p.ProjectInstance.FullPath))
            .ToHashSet(PathUtilities.OSSpecificPathComparer);

    public IReadOnlySet<string> BuildFiles
        => _lazyBuildFiles.Value;

    public IReadOnlyDictionary<ProjectInstanceId, StaticWebAssetsManifest> StaticWebAssetsManifests
        => staticWebAssetsManifests;

    public void WatchFiles(FileWatcher fileWatcher)
    {
        fileWatcher.WatchContainingDirectories(Files.Keys, includeSubdirectories: true);

        fileWatcher.WatchContainingDirectories(
            StaticWebAssetsManifests.Values.SelectMany(static manifest => manifest.DiscoveryPatterns.Select(static pattern => pattern.Directory)),
            includeSubdirectories: true);

        fileWatcher.WatchFiles(BuildFiles);
    }

    public static ImmutableDictionary<string, string> GetGlobalBuildOptions(IEnumerable<string> buildArguments, EnvironmentOptions environmentOptions)
    {
        // See https://github.com/dotnet/project-system/blob/main/docs/well-known-project-properties.md

        return BuildUtilities.ParseBuildProperties(buildArguments)
            .ToImmutableDictionary(keySelector: arg => arg.key, elementSelector: arg => arg.value)
            .SetItem(PropertyNames.DotNetWatchBuild, "true")
            .SetItem(PropertyNames.DesignTimeBuild, "true")
            .SetItem(PropertyNames.SkipCompilerExecution, "true")
            .SetItem(PropertyNames.ProvideCommandLineArgs, "true");
    }

    /// <summary>
    /// Loads project graph and performs design-time build.
    /// </summary>
    public static EvaluationResult? TryCreate(
        ProjectGraphFactory factory,
        string rootProjectPath,
        ILogger logger,
        GlobalOptions options,
        EnvironmentOptions environmentOptions,
        bool restore,
        CancellationToken cancellationToken)
    {
        var buildReporter = new BuildReporter(logger, options, environmentOptions);

        var projectGraph = factory.TryLoadProjectGraph(
            rootProjectPath,
            logger,
            projectGraphRequired: true,
            cancellationToken);

        if (projectGraph == null)
        {
            return null;
        }

        var rootNode = projectGraph.GraphRoots.Single();

        if (restore)
        {
            using (var loggers = buildReporter.GetLoggers(rootNode.ProjectInstance.FullPath, "Restore"))
            {
                if (!rootNode.ProjectInstance.Build([TargetNames.Restore], loggers))
                {
                    logger.LogError("Failed to restore project '{Path}'.", rootProjectPath);
                    loggers.ReportOutput();
                    return null;
                }
            }
        }

        var fileItems = new Dictionary<string, FileItem>();
        var staticWebAssetManifests = new Dictionary<ProjectInstanceId, StaticWebAssetsManifest>();

        foreach (var project in projectGraph.ProjectNodesTopologicallySorted)
        {
            // Deep copy so that we can reuse the graph for building additional targets later on.
            // If we didn't copy the instance the targets might duplicate items that were already
            // populated by design-time build.
            var projectInstance = project.ProjectInstance.DeepCopy();

            // skip outer build project nodes:
            if (projectInstance.GetPropertyValue(PropertyNames.TargetFramework) == "")
            {
                continue;
            }

            var targets = GetBuildTargets(projectInstance, environmentOptions);
            if (targets is [])
            {
                continue;
            }

            using (var loggers = buildReporter.GetLoggers(projectInstance.FullPath, "DesignTimeBuild"))
            {
                if (!projectInstance.Build(targets, loggers))
                {
                    logger.LogError("Failed to build project '{Path}'.", projectInstance.FullPath);
                    loggers.ReportOutput();
                    return null;
                }
            }

            var projectPath = projectInstance.FullPath;
            var projectDirectory = Path.GetDirectoryName(projectPath)!;

            if (targets.Contains(TargetNames.GenerateComputedBuildStaticWebAssets) &&
                projectInstance.GetIntermediateOutputDirectory() is { } outputDir &&
                StaticWebAssetsManifest.TryParseFile(Path.Combine(outputDir, StaticWebAsset.ManifestFileName), logger) is { } manifest)
            {
                staticWebAssetManifests.Add(projectInstance.GetId(), manifest);

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
            if (targets.Contains(TargetNames.ResolveScopedCssInputs))
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

                if (!fileItems.TryGetValue(filePath, out var existingFile))
                {
                    fileItems.Add(filePath, new FileItem
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

        buildReporter.ReportWatchedFiles(fileItems);

        return new EvaluationResult(projectGraph, fileItems, staticWebAssetManifests);
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
