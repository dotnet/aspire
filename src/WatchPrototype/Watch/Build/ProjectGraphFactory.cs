// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Graph;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Microsoft.DotNet.Watch;

internal sealed class ProjectGraphFactory(ImmutableDictionary<string, string> globalOptions)
{
    /// <summary>
    /// Reuse <see cref="ProjectCollection"/> with XML element caching to improve performance.
    ///
    /// The cache is automatically updated when build files change.
    /// https://github.com/dotnet/msbuild/blob/b6f853defccd64ae1e9c7cf140e7e4de68bff07c/src/Build/Definition/ProjectCollection.cs#L343-L354
    /// </summary>
    private readonly ProjectCollection _collection = new(
        globalProperties: globalOptions,
        loggers: [],
        remoteLoggers: [],
        ToolsetDefinitionLocations.Default,
        maxNodeCount: 1,
        onlyLogCriticalEvents: false,
        loadProjectsReadOnly: false,
        useAsynchronousLogging: false,
        reuseProjectRootElementCache: true);

    /// <summary>
    /// Tries to create a project graph by running the build evaluation phase on the <paramref name="rootProjectFile"/>.
    /// </summary>
    public ProjectGraph? TryLoadProjectGraph(
        string rootProjectFile,
        ILogger logger,
        bool projectGraphRequired,
        CancellationToken cancellationToken)
    {
        var entryPoint = new ProjectGraphEntryPoint(rootProjectFile, globalOptions);
        try
        {
            return new ProjectGraph([entryPoint], _collection, projectInstanceFactory: null, cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            // ProejctGraph aggregates OperationCanceledException exception,
            // throw here to propagate the cancellation.
            cancellationToken.ThrowIfCancellationRequested();

            logger.LogDebug("Failed to load project graph.");

            if (e is AggregateException { InnerExceptions: var innerExceptions })
            {
                foreach (var inner in innerExceptions)
                {
                    Report(inner);
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
}
