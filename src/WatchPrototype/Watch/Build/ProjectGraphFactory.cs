// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Graph;
using Microsoft.DotNet.ProjectTools;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Microsoft.DotNet.Watch;

internal sealed class ProjectGraphFactory
{
    /// <summary>
    /// Reuse <see cref="ProjectCollection"/> with XML element caching to improve performance.
    ///
    /// The cache is automatically updated when build files change.
    /// https://github.com/dotnet/msbuild/blob/b6f853defccd64ae1e9c7cf140e7e4de68bff07c/src/Build/Definition/ProjectCollection.cs#L343-L354
    /// </summary>
    private readonly ProjectCollection _collection;

    private readonly ImmutableDictionary<string, string> _globalOptions;
    private readonly ProjectRepresentation _rootProject;

    // Only the root project can be virtual. #:project does not support targeting other single-file projects.
    private readonly VirtualProjectBuilder? _virtualRootProjectBuilder;

    public ProjectGraphFactory(
        ProjectRepresentation rootProject,
        string? targetFramework,
        ImmutableDictionary<string, string> globalOptions)
    {
        _collection = new(
            globalProperties: globalOptions,
            loggers: [],
            remoteLoggers: [],
            ToolsetDefinitionLocations.Default,
            maxNodeCount: 1,
            onlyLogCriticalEvents: false,
            loadProjectsReadOnly: false,
            useAsynchronousLogging: false,
            reuseProjectRootElementCache: true);

        _globalOptions = globalOptions;
        _rootProject = rootProject;

        if (rootProject.EntryPointFilePath != null)
        {
            _virtualRootProjectBuilder = new VirtualProjectBuilder(rootProject.EntryPointFilePath, targetFramework ?? GetProductTargetFramework());
        }
    }

    private static string GetProductTargetFramework()
    {
        var attribute = typeof(VirtualProjectBuilder).Assembly.GetCustomAttribute<TargetFrameworkAttribute>() ?? throw new InvalidOperationException();
        var version = new FrameworkName(attribute.FrameworkName).Version;
        return $"net{version.Major}.{version.Minor}";
    }

    /// <summary>
    /// Tries to create a project graph by running the build evaluation phase on the <see cref="_rootProject"/>.
    /// </summary>
    public ProjectGraph? TryLoadProjectGraph(
        ILogger logger,
        bool projectGraphRequired,
        CancellationToken cancellationToken)
    {
        var entryPoint = new ProjectGraphEntryPoint(_rootProject.ProjectGraphPath, _globalOptions);
        try
        {
            return new ProjectGraph([entryPoint], _collection, (path, globalProperties, collection) => CreateProjectInstance(path, globalProperties, collection, logger), cancellationToken);
        }
        catch (ProjectCreationFailedException)
        {
            // Errors have already been reported.
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            // ProjectGraph aggregates OperationCanceledException exception,
            // throw here to propagate the cancellation.
            cancellationToken.ThrowIfCancellationRequested();

            logger.LogDebug("Failed to load project graph.");

            if (e is AggregateException { InnerExceptions: var innerExceptions })
            {
                foreach (var inner in innerExceptions)
                {
                    if (inner is not ProjectCreationFailedException)
                    {
                        Report(inner);
                    }
                }
            }
            else
            {
                Report(e);
            }

            void Report(Exception e)
            {
                if (projectGraphRequired)
                {
                    logger.LogError(e.Message);
                }
                else
                {
                    logger.LogWarning(e.Message);
                }
            }
        }

        return null;
    }

    private ProjectInstance CreateProjectInstance(string projectPath, Dictionary<string, string> globalProperties, ProjectCollection projectCollection, ILogger logger)
    {
        if (_virtualRootProjectBuilder != null && projectPath == _rootProject.ProjectGraphPath)
        {
            var anyError = false;

            _virtualRootProjectBuilder.CreateProjectInstance(
                projectCollection,
                (sourceFile, textSpan, message) =>
                {
                    anyError = true;
                    logger.LogError("{Location}: {Message}", sourceFile.GetLocationString(textSpan), message);
                },
                out var projectInstance,
                out _);

            if (anyError)
            {
                throw new ProjectCreationFailedException();
            }

            return projectInstance;
        }

        return new ProjectInstance(
            projectPath,
            globalProperties,
            toolsVersion: "Current",
            subToolsetVersion: null,
            projectCollection);
    }

    private sealed class ProjectCreationFailedException() : Exception();
}
