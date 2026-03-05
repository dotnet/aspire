// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;

namespace Microsoft.DotNet.Watch;

internal sealed class ProjectBuildManager(ProjectCollection collection, BuildReporter reporter)
{
    /// <summary>
    /// Semaphore that ensures we only start one build build at a time per process, which is required by MSBuild.
    /// </summary>
    private static readonly SemaphoreSlim s_buildSemaphore = new(initialCount: 1);

    private static readonly IReadOnlyDictionary<string, TargetResult> s_emptyTargetResults = new Dictionary<string, TargetResult>();

    public readonly ProjectCollection Collection = collection;
    public readonly BuildReporter BuildReporter = reporter;

    /// <summary>
    /// Used by tests to ensure no more than one build is running at a time, which is required by MSBuild.
    /// </summary>
    internal static SemaphoreSlim Test_BuildSemaphore
        => s_buildSemaphore;

    /// <summary>
    /// Executes the specified build requests.
    /// </summary>
    /// <param name="onFailure">Invoked for each project that fails to build. Returns true to continue build or false to cancel.</param>
    /// <returns>True if all projects built successfully.</returns>
    public async Task<ImmutableArray<BuildResult<T>>> BuildAsync<T>(
        IReadOnlyList<BuildRequest<T>> requests,
        Func<ProjectInstance, bool> onFailure,
        string operationName,
        CancellationToken cancellationToken)
    {
        Debug.Assert(requests is not []);
        var buildRequests = requests.Select(r => new BuildRequestData(r.ProjectInstance, [.. r.Targets])).ToArray();

        using var loggers = BuildReporter.GetLoggers(buildRequests[0].ProjectFullPath, operationName);

        using var buildCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        await s_buildSemaphore.WaitAsync(cancellationToken);

        var manager = BuildManager.DefaultBuildManager;
        using var _ = buildCancellationTokenSource.Token.Register(manager.CancelAllSubmissions);

        var buildParameters = new BuildParameters(Collection)
        {
            Loggers = loggers,
        };

        manager.BeginBuild(buildParameters);
        try
        {
            var buildTasks = new List<Task<BuildResult?>>(buildRequests.Length);

            foreach (var request in buildRequests)
            {
                var taskSource = new TaskCompletionSource<BuildResult?>();

                // Queues the build request and immediately returns. The callback is executed when the build completes.
                manager.PendBuildRequest(request).ExecuteAsync(
                    callback: submission =>
                    {
                        // Cancel on first failure:
                        if (submission.BuildResult?.OverallResult != BuildResultCode.Success)
                        {
                            var projectInstance = (ProjectInstance)submission.AsyncContext!;

                            var continueBuild = onFailure(projectInstance);
                            if (!continueBuild)
                            {
                                buildCancellationTokenSource.Cancel();
                                taskSource.SetCanceled();
                                return;
                            }
                        }

                        taskSource.SetResult(submission.BuildResult);
                    },
                    context: request.ProjectInstance);

                buildTasks.Add(taskSource.Task);
            }

            var results = await Task.WhenAll(buildTasks);

            return [.. results.Select((result, index) => new BuildResult<T>(
                (IReadOnlyDictionary<string, TargetResult>?)result?.ResultsByTarget ?? s_emptyTargetResults,
                requests[index].ProjectInstance,
                requests[index].Data))];
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // build was canceled
            loggers.ReportOutput();
            return [];
        }
        finally
        {
            manager.EndBuild();
            s_buildSemaphore.Release();
        }
    }
}
