// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Build.Execution;
using Microsoft.Build.Graph;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.ExternalAccess.HotReload.Api;
using Microsoft.DotNet.HotReload;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch
{
    internal sealed class CompilationHandler : IDisposable
    {
        public readonly HotReloadMSBuildWorkspace Workspace;
        private readonly DotNetWatchContext _context;
        private readonly HotReloadService _hotReloadService;

        /// <summary>
        /// Lock to synchronize:
        /// <see cref="_runningProjects"/>
        /// <see cref="_previousUpdates"/>
        /// </summary>
        private readonly object _runningProjectsAndUpdatesGuard = new();

        /// <summary>
        /// Projects that have been launched and to which we apply changes.
        /// </summary>
        private ImmutableDictionary<string, ImmutableArray<RunningProject>> _runningProjects = ImmutableDictionary<string, ImmutableArray<RunningProject>>.Empty;

        /// <summary>
        /// All updates that were attempted. Includes updates whose application failed.
        /// </summary>
        private ImmutableList<HotReloadService.Update> _previousUpdates = [];

        private bool _isDisposed;
        private int _solutionUpdateId;

        /// <summary>
        /// Current set of project instances indexed by <see cref="ProjectInstance.FullPath"/>.
        /// Updated whenever the project graph changes.
        /// </summary>
        private ImmutableDictionary<string, ImmutableArray<ProjectInstance>> _projectInstances = [];

        public CompilationHandler(DotNetWatchContext context)
        {
            _context = context;
            Workspace = new HotReloadMSBuildWorkspace(context.Logger, projectFile => (instances: _projectInstances.GetValueOrDefault(projectFile, []), project: null));
            _hotReloadService = new HotReloadService(Workspace.CurrentSolution.Services, () => ValueTask.FromResult(GetAggregateCapabilities()));
        }

        public void Dispose()
        {
            _isDisposed = true;
            Workspace?.Dispose();
        }

        private ILogger Logger
            => _context.Logger;

        public async ValueTask TerminateNonRootProcessesAndDispose(CancellationToken cancellationToken)
        {
            Logger.LogDebug("Terminating remaining child processes.");
            await TerminateNonRootProcessesAsync(projectPaths: null, cancellationToken);
            Dispose();
        }

        private void DiscardPreviousUpdates(ImmutableArray<ProjectId> projectsToBeRebuilt)
        {
            // Remove previous updates to all modules that were affected by rude edits.
            // All running projects that statically reference these modules have been terminated.
            // If we missed any project that dynamically references one of these modules its rebuild will fail.
            // At this point there is thus no process that these modules loaded and any process created in future
            // that will load their rebuilt versions.

            lock (_runningProjectsAndUpdatesGuard)
            {
                _previousUpdates = _previousUpdates.RemoveAll(update => projectsToBeRebuilt.Contains(update.ProjectId));
            }
        }
        public async ValueTask StartSessionAsync(CancellationToken cancellationToken)
        {
            Logger.Log(MessageDescriptor.HotReloadSessionStarting);

            var solution = Workspace.CurrentSolution;

            await _hotReloadService.StartSessionAsync(solution, cancellationToken);

            // TODO: StartSessionAsync should do this: https://github.com/dotnet/roslyn/issues/80687
            foreach (var project in solution.Projects)
            {
                foreach (var document in project.AdditionalDocuments)
                {
                    await document.GetTextAsync(cancellationToken);
                }

                foreach (var document in project.AnalyzerConfigDocuments)
                {
                    await document.GetTextAsync(cancellationToken);
                }
            }

            Logger.Log(MessageDescriptor.HotReloadSessionStarted);
        }

        public async Task<RunningProject?> TrackRunningProjectAsync(
            ProjectGraphNode projectNode,
            ProjectOptions projectOptions,
            HotReloadClients clients,
            ProcessSpec processSpec,
            RestartOperation restartOperation,
            CancellationTokenSource processTerminationSource,
            CancellationToken cancellationToken)
        {
            var processExitedSource = new CancellationTokenSource();

            // Cancel process communication as soon as process termination is requested, shutdown is requested, or the process exits (whichever comes first).
            // If we only cancel after we process exit event handler is triggered the pipe might have already been closed and may fail unexpectedly.
            using var processCommunicationCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(processTerminationSource.Token, processExitedSource.Token, cancellationToken);
            var processCommunicationCancellationToken = processCommunicationCancellationSource.Token;

            // Dispose these objects on failure:
            using var disposables = new Disposables([clients, processExitedSource]);

            // It is important to first create the named pipe connection (Hot Reload client is the named pipe server)
            // and then start the process (named pipe client). Otherwise, the connection would fail.
            clients.InitiateConnection(processCommunicationCancellationToken);

            RunningProject? publishedRunningProject = null;

            var previousOnExit = processSpec.OnExit;
            processSpec.OnExit = async (processId, exitCode) =>
            {
                // Await the previous action so that we only clean up after all requested "on exit" actions have been completed.
                if (previousOnExit != null)
                {
                    await previousOnExit(processId, exitCode);
                }

                // Remove the running project if it has been published to _runningProjects (if it hasn't exited during initialization):
                if (publishedRunningProject != null && RemoveRunningProject(publishedRunningProject))
                {
                    publishedRunningProject.Dispose();
                }
            };

            var launchResult = new ProcessLaunchResult();
            var runningProcess = _context.ProcessRunner.RunAsync(processSpec, clients.ClientLogger, launchResult, processTerminationSource.Token);
            if (launchResult.ProcessId == null)
            {
                // error already reported
                return null;
            }

            var projectPath = projectNode.ProjectInstance.FullPath;

            try
            {
                // Wait for agent to create the name pipe and send capabilities over.
                // the agent blocks the app execution until initial updates are applied (if any).
                var capabilities = await clients.GetUpdateCapabilitiesAsync(processCommunicationCancellationToken);

                var runningProject = new RunningProject(
                    projectNode,
                    projectOptions,
                    clients,
                    runningProcess,
                    launchResult.ProcessId.Value,
                    processExitedSource: processExitedSource,
                    processTerminationSource: processTerminationSource,
                    restartOperation: restartOperation,
                    capabilities);

                // ownership transferred to running project:
                disposables.Items.Clear();
                disposables.Items.Add(runningProject);

                var appliedUpdateCount = 0;
                while (true)
                {
                    // Observe updates that need to be applied to the new process
                    // and apply them before adding it to running processes.
                    // Do not block on udpates being made to other processes to avoid delaying the new process being up-to-date.
                    var updatesToApply = _previousUpdates.Skip(appliedUpdateCount).ToImmutableArray();
                    if (updatesToApply.Any())
                    {
                        await await clients.ApplyManagedCodeUpdatesAsync(
                            ToManagedCodeUpdates(updatesToApply),
                            applyOperationCancellationToken: processExitedSource.Token,
                            cancellationToken: processCommunicationCancellationToken);
                    }

                    appliedUpdateCount += updatesToApply.Length;

                    lock (_runningProjectsAndUpdatesGuard)
                    {
                        ObjectDisposedException.ThrowIf(_isDisposed, this);

                        // More updates might have come in while we have been applying updates.
                        // If so, continue updating.
                        if (_previousUpdates.Count > appliedUpdateCount)
                        {
                            continue;
                        }

                        // Only add the running process after it has been up-to-date.
                        // This will prevent new updates being applied before we have applied all the previous updates.
                        if (!_runningProjects.TryGetValue(projectPath, out var projectInstances))
                        {
                            projectInstances = [];
                        }

                        _runningProjects = _runningProjects.SetItem(projectPath, projectInstances.Add(runningProject));

                        // ownership transferred to _runningProjects
                        publishedRunningProject = runningProject;
                        disposables.Items.Clear();
                        break;
                    }
                }

                clients.OnRuntimeRudeEdit += (code, message) =>
                {
                    // fire and forget:
                    _ = HandleRuntimeRudeEditAsync(runningProject, message, cancellationToken);
                };

                // Notifies the agent that it can unblock the execution of the process:
                await clients.InitialUpdatesAppliedAsync(processCommunicationCancellationToken);

                // If non-empty solution is loaded into the workspace (a Hot Reload session is active):
                if (Workspace.CurrentSolution is { ProjectIds: not [] } currentSolution)
                {
                    // Preparing the compilation is a perf optimization. We can skip it if the session hasn't been started yet.
                    PrepareCompilations(currentSolution, projectPath, cancellationToken);
                }

                return runningProject;
            }
            catch (OperationCanceledException) when (processExitedSource.IsCancellationRequested)
            {
                // Process exited during initialization. This should not happen since we control the process during this time.
                Logger.LogError("Failed to launch '{ProjectPath}'. Process {PID} exited during initialization.", projectPath, launchResult.ProcessId);
                return null;
            }
        }

        private async Task HandleRuntimeRudeEditAsync(RunningProject runningProject, string rudeEditMessage, CancellationToken cancellationToken)
        {
            var logger = runningProject.Clients.ClientLogger;

            try
            {
                // Always auto-restart on runtime rude edits regardless of the settings.
                // Since there is no debugger attached the process would crash on an unhandled HotReloadException if
                // we let it continue executing.
                logger.LogWarning(rudeEditMessage);
                logger.Log(MessageDescriptor.RestartingApplication);

                if (!runningProject.InitiateRestart())
                {
                    // Already in the process of restarting, possibly because of another runtime rude edit.
                    return;
                }

                await runningProject.Clients.ReportCompilationErrorsInApplicationAsync([rudeEditMessage, MessageDescriptor.RestartingApplication.GetMessage()], cancellationToken);

                // Terminate the process.
                await runningProject.TerminateAsync();

                // Creates a new running project and launches it:
                await runningProject.RestartOperation(cancellationToken);
            }
            catch (Exception e)
            {
                if (e is not OperationCanceledException)
                {
                    logger.LogError("Failed to handle runtime rude edit: {Exception}", e.ToString());
                }
            }
        }

        private ImmutableArray<string> GetAggregateCapabilities()
        {
            var capabilities = _runningProjects
                .SelectMany(p => p.Value)
                .SelectMany(p => p.Capabilities)
                .Distinct(StringComparer.Ordinal)
                .Order()
                .ToImmutableArray();

            Logger.Log(MessageDescriptor.HotReloadCapabilities, string.Join(" ", capabilities));
            return capabilities;
        }

        private static void PrepareCompilations(Solution solution, string projectPath, CancellationToken cancellationToken)
        {
            // Warm up the compilation. This would help make the deltas for first edit appear much more quickly
            foreach (var project in solution.Projects)
            {
                if (project.FilePath == projectPath)
                {
                    // fire and forget:
                    _ = project.GetCompilationAsync(cancellationToken);
                }
            }
        }

        public async ValueTask<(
                ImmutableArray<HotReloadService.Update> projectUpdates,
                ImmutableArray<string> projectsToRebuild,
                ImmutableArray<string> projectsToRedeploy,
                ImmutableArray<RunningProject> projectsToRestart)> HandleManagedCodeChangesAsync(
            bool autoRestart,
            Func<IEnumerable<string>, CancellationToken, Task<bool>> restartPrompt,
            CancellationToken cancellationToken)
        {
            var currentSolution = Workspace.CurrentSolution;
            var runningProjects = _runningProjects;

            var runningProjectInfos =
               (from project in currentSolution.Projects
                let runningProject = GetCorrespondingRunningProject(project, runningProjects)
                where runningProject != null
                let autoRestartProject = autoRestart || runningProject.ProjectNode.IsAutoRestartEnabled()
                select (project.Id, info: new HotReloadService.RunningProjectInfo() { RestartWhenChangesHaveNoEffect = autoRestartProject }))
                .ToImmutableDictionary(e => e.Id, e => e.info);

            var updates = await _hotReloadService.GetUpdatesAsync(currentSolution, runningProjectInfos, cancellationToken);

            await DisplayResultsAsync(updates, runningProjectInfos, cancellationToken);

            if (updates.Status is HotReloadService.Status.NoChangesToApply or HotReloadService.Status.Blocked)
            {
                // If Hot Reload is blocked (due to compilation error) we ignore the current
                // changes and await the next file change.

                // Note: CommitUpdate/DiscardUpdate is not expected to be called.
                return ([], [], [], []);
            }

            var projectsToPromptForRestart =
                (from projectId in updates.ProjectsToRestart.Keys
                 where !runningProjectInfos[projectId].RestartWhenChangesHaveNoEffect // equivallent to auto-restart
                 select currentSolution.GetProject(projectId)!.Name).ToList();

            if (projectsToPromptForRestart.Any() &&
                !await restartPrompt.Invoke(projectsToPromptForRestart, cancellationToken))
            {
                _hotReloadService.DiscardUpdate();

                Logger.Log(MessageDescriptor.HotReloadSuspended);
                await Task.Delay(-1, cancellationToken);

                return ([], [], [], []);
            }

            // Note: Releases locked project baseline readers, so we can rebuild any projects that need rebuilding.
            _hotReloadService.CommitUpdate();

            DiscardPreviousUpdates(updates.ProjectsToRebuild);

            var projectsToRebuild = updates.ProjectsToRebuild.Select(id => currentSolution.GetProject(id)!.FilePath!).ToImmutableArray();
            var projectsToRedeploy = updates.ProjectsToRedeploy.Select(id => currentSolution.GetProject(id)!.FilePath!).ToImmutableArray();

            // Terminate all tracked processes that need to be restarted,
            // except for the root process, which will terminate later on.
            var projectsToRestart = updates.ProjectsToRestart.IsEmpty
                ? []
                : await TerminateNonRootProcessesAsync(updates.ProjectsToRestart.Select(e => currentSolution.GetProject(e.Key)!.FilePath!), cancellationToken);

            return (updates.ProjectUpdates, projectsToRebuild, projectsToRedeploy, projectsToRestart);
        }

        public async ValueTask ApplyUpdatesAsync(ImmutableArray<HotReloadService.Update> updates, Stopwatch stopwatch, CancellationToken cancellationToken)
        {
            Debug.Assert(!updates.IsEmpty);

            ImmutableDictionary<string, ImmutableArray<RunningProject>> projectsToUpdate;
            lock (_runningProjectsAndUpdatesGuard)
            {
                // Adding the updates makes sure that all new processes receive them before they are added to running processes.
                _previousUpdates = _previousUpdates.AddRange(updates);

                // Capture the set of processes that do not have the currently calculated deltas yet.
                projectsToUpdate = _runningProjects;
            }

            // Apply changes to all running projects, even if they do not have a static project dependency on any project that changed.
            // The process may load any of the binaries using MEF or some other runtime dependency loader.

            var applyTasks = new List<Task>();

            foreach (var (_, projects) in projectsToUpdate)
            {
                foreach (var runningProject in projects)
                {
                    // Only cancel applying updates when the process exits. Canceling disables further updates since the state of the runtime becomes unknown.
                    var applyTask = await runningProject.Clients.ApplyManagedCodeUpdatesAsync(
                        ToManagedCodeUpdates(updates),
                        applyOperationCancellationToken: runningProject.ProcessExitedCancellationToken,
                        cancellationToken);

                    applyTasks.Add(runningProject.CompleteApplyOperationAsync(applyTask));
                }
            }

            // fire and forget:
            _ = CompleteApplyOperationAsync(applyTasks, stopwatch, MessageDescriptor.ManagedCodeChangesApplied);
        }

        private async Task CompleteApplyOperationAsync(IEnumerable<Task> applyTasks, Stopwatch stopwatch, MessageDescriptor message)
        {
            try
            {
                await Task.WhenAll(applyTasks);

                _context.Logger.Log(message, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception e)
            {
                // Handle all exceptions since this is a fire-and-forget task.

                if (e is not OperationCanceledException)
                {
                    _context.Logger.LogError("Failed to apply updates: {Exception}", e.ToString());
                }
            }
        }

        private static RunningProject? GetCorrespondingRunningProject(Project project, ImmutableDictionary<string, ImmutableArray<RunningProject>> runningProjects)
        {
            if (project.FilePath == null || !runningProjects.TryGetValue(project.FilePath, out var projectsWithPath))
            {
                return null;
            }

            // msbuild workspace doesn't set TFM if the project is not multi-targeted
            var tfm = HotReloadService.GetTargetFramework(project);
            if (tfm == null)
            {
                return projectsWithPath[0];
            }

            return projectsWithPath.SingleOrDefault(p => string.Equals(p.ProjectNode.ProjectInstance.GetTargetFramework(), tfm, StringComparison.OrdinalIgnoreCase));
        }

        private async ValueTask DisplayResultsAsync(HotReloadService.Updates updates, ImmutableDictionary<ProjectId, HotReloadService.RunningProjectInfo> runningProjectInfos, CancellationToken cancellationToken)
        {
            switch (updates.Status)
            {
                case HotReloadService.Status.ReadyToApply:
                    break;

                case HotReloadService.Status.NoChangesToApply:
                    Logger.Log(MessageDescriptor.NoCSharpChangesToApply);
                    break;

                case HotReloadService.Status.Blocked:
                    Logger.Log(MessageDescriptor.UnableToApplyChanges);
                    break;

                default:
                    throw new InvalidOperationException();
            }

            if (!updates.ProjectsToRestart.IsEmpty)
            {
                Logger.Log(MessageDescriptor.RestartNeededToApplyChanges);
            }

            var errorsToDisplayInApp = new List<string>();

            // Display errors first, then warnings:
            ReportCompilationDiagnostics(DiagnosticSeverity.Error);
            ReportCompilationDiagnostics(DiagnosticSeverity.Warning);
            ReportRudeEdits();

            // report or clear diagnostics in the browser UI
            await ForEachProjectAsync(
                _runningProjects,
                (project, cancellationToken) => project.Clients.ReportCompilationErrorsInApplicationAsync([.. errorsToDisplayInApp], cancellationToken).AsTask() ?? Task.CompletedTask,
                cancellationToken);

            void ReportCompilationDiagnostics(DiagnosticSeverity severity)
            {
                foreach (var diagnostic in updates.PersistentDiagnostics)
                {
                    if (diagnostic.Id == "CS8002")
                    {
                        // TODO: This is not a useful warning. Compiler shouldn't be reporting this on .NET/
                        // Referenced assembly '...' does not have a strong name"
                        continue;
                    }

                    // TODO: https://github.com/dotnet/roslyn/pull/79018
                    // shouldn't be included in compilation diagnostics
                    if (diagnostic.Id == "ENC0118")
                    {
                        // warning ENC0118: Changing 'top-level code' might not have any effect until the application is restarted
                        continue;
                    }

                    if (diagnostic.DefaultSeverity != severity)
                    {
                        continue;
                    }

                    ReportDiagnostic(diagnostic, GetMessageDescriptor(diagnostic, verbose: false));
                }
            }

            void ReportRudeEdits()
            {
                // Rude edits in projects that caused restart of a project that can be restarted automatically
                // will be reported only as verbose output.
                var projectsRestartedDueToRudeEdits = updates.ProjectsToRestart
                    .Where(e => IsAutoRestartEnabled(e.Key))
                    .SelectMany(e => e.Value)
                    .ToHashSet();

                // Project with rude edit that doesn't impact running project is only listed in ProjectsToRebuild.
                // Such projects are always auto-rebuilt whether or not there is any project to be restarted that needs a confirmation.
                var projectsRebuiltDueToRudeEdits = updates.ProjectsToRebuild
                    .Where(p => !updates.ProjectsToRestart.ContainsKey(p))
                    .ToHashSet();

                foreach (var (projectId, diagnostics) in updates.TransientDiagnostics)
                {
                    foreach (var diagnostic in diagnostics)
                    {
                        var prefix =
                            projectsRestartedDueToRudeEdits.Contains(projectId) ? "[auto-restart] " :
                            projectsRebuiltDueToRudeEdits.Contains(projectId) ? "[auto-rebuild] " :
                            "";

                        var descriptor = GetMessageDescriptor(diagnostic, verbose: prefix != "");
                        ReportDiagnostic(diagnostic, descriptor, prefix);
                    }
                }
            }

            bool IsAutoRestartEnabled(ProjectId id)
                => runningProjectInfos.TryGetValue(id, out var info) && info.RestartWhenChangesHaveNoEffect;

            void ReportDiagnostic(Diagnostic diagnostic, MessageDescriptor descriptor, string autoPrefix = "")
            {
                var display = CSharpDiagnosticFormatter.Instance.Format(diagnostic);
                var args = new[] { autoPrefix, display };

                Logger.Log(descriptor, args);

                if (autoPrefix != "")
                {
                    errorsToDisplayInApp.Add(MessageDescriptor.RestartingApplicationToApplyChanges.GetMessage());
                }
                else if (descriptor.Level != LogLevel.None)
                {
                    errorsToDisplayInApp.Add(descriptor.GetMessage(args));
                }
            }

            // Use the default severity of the diagnostic as it conveys impact on Hot Reload
            // (ignore warnings as errors and other severity configuration).
            static MessageDescriptor GetMessageDescriptor(Diagnostic diagnostic, bool verbose)
            {
                if (verbose)
                {
                    return MessageDescriptor.ApplyUpdate_Verbose;
                }

                if (diagnostic.Id == "ENC0118")
                {
                    // Changing '<entry-point>' might not have any effect until the application is restarted.
                    return MessageDescriptor.ApplyUpdate_ChangingEntryPoint;
                }

                return diagnostic.DefaultSeverity switch
                {
                    DiagnosticSeverity.Error => MessageDescriptor.ApplyUpdate_Error,
                    DiagnosticSeverity.Warning => MessageDescriptor.ApplyUpdate_Warning,
                    _ => MessageDescriptor.ApplyUpdate_Verbose,
                };
            }
        }

        private static readonly string[] s_targets = [TargetNames.GenerateComputedBuildStaticWebAssets, TargetNames.ResolveReferencedProjectsStaticWebAssets];

        private static bool HasScopedCssTargets(ProjectInstance projectInstance)
            => s_targets.All(projectInstance.Targets.ContainsKey);

        public async ValueTask HandleStaticAssetChangesAsync(
            IReadOnlyList<ChangedFile> files,
            ProjectNodeMap projectMap,
            IReadOnlyDictionary<ProjectInstanceId, StaticWebAssetsManifest> manifests,
            Stopwatch stopwatch,
            CancellationToken cancellationToken)
        {
            var assets = new Dictionary<ProjectInstance, Dictionary<string, StaticWebAsset>>();
            var projectInstancesToRegenerate = new HashSet<ProjectInstance>();

            foreach (var changedFile in files)
            {
                var file = changedFile.Item;
                var isScopedCss = StaticWebAsset.IsScopedCssFile(file.FilePath);

                if (!isScopedCss && file.StaticWebAssetRelativeUrl is null)
                {
                    continue;
                }

                foreach (var containingProjectPath in file.ContainingProjectPaths)
                {
                    if (!projectMap.Map.TryGetValue(containingProjectPath, out var containingProjectNodes))
                    {
                        // Shouldn't happen.
                        Logger.LogWarning("Project '{Path}' not found in the project graph.", containingProjectPath);
                        continue;
                    }

                    foreach (var containingProjectNode in containingProjectNodes)
                    {
                        if (isScopedCss)
                        {
                            // The outer build project instance(that specifies TargetFrameworks) won't have the target.
                            if (!HasScopedCssTargets(containingProjectNode.ProjectInstance))
                            {
                                continue;
                            }

                            projectInstancesToRegenerate.Add(containingProjectNode.ProjectInstance);
                        }

                        foreach (var referencingProjectNode in containingProjectNode.GetAncestorsAndSelf())
                        {
                            var applicationProjectInstance = referencingProjectNode.ProjectInstance;
                            if (!TryGetRunningProject(applicationProjectInstance.FullPath, out _))
                            {
                                continue;
                            }

                            string filePath;
                            string relativeUrl;

                            if (isScopedCss)
                            {
                                // Razor class library may be referenced by application that does not have static assets:
                                if (!HasScopedCssTargets(applicationProjectInstance))
                                {
                                    continue;
                                }

                                projectInstancesToRegenerate.Add(applicationProjectInstance);

                                var bundleFileName = StaticWebAsset.GetScopedCssBundleFileName(
                                    applicationProjectFilePath: applicationProjectInstance.FullPath,
                                    containingProjectFilePath: containingProjectNode.ProjectInstance.FullPath);

                                if (!manifests.TryGetValue(applicationProjectInstance.GetId(), out var manifest))
                                {
                                    // Shouldn't happen.
                                    Logger.LogWarning("[{Project}] Static web asset manifest not found.", containingProjectNode.GetDisplayName());
                                    continue;
                                }

                                if (!manifest.TryGetBundleFilePath(bundleFileName, out var bundleFilePath))
                                {
                                    // Shouldn't happen.
                                    Logger.LogWarning("[{Project}] Scoped CSS bundle file '{BundleFile}' not found.", containingProjectNode.GetDisplayName(), bundleFileName);
                                    continue;
                                }

                                filePath = bundleFilePath;
                                relativeUrl = bundleFileName;
                            }
                            else
                            {
                                Debug.Assert(file.StaticWebAssetRelativeUrl != null);
                                filePath = file.FilePath;
                                relativeUrl = file.StaticWebAssetRelativeUrl;
                            }

                            if (!assets.TryGetValue(applicationProjectInstance, out var applicationAssets))
                            {
                                applicationAssets = [];
                                assets.Add(applicationProjectInstance, applicationAssets);
                            }
                            else if (applicationAssets.ContainsKey(filePath))
                            {
                                // asset already being updated in this application project:
                                continue;
                            }

                            applicationAssets.Add(filePath, new StaticWebAsset(
                                filePath,
                                StaticWebAsset.WebRoot + "/" + relativeUrl,
                                containingProjectNode.GetAssemblyName(),
                                isApplicationProject: containingProjectNode.ProjectInstance == applicationProjectInstance));
                        }
                    }
                }
            }

            if (assets.Count == 0)
            {
                return;
            }

            HashSet<ProjectInstance>? failedApplicationProjectInstances = null;
            if (projectInstancesToRegenerate.Count > 0)
            {
                var buildReporter = new BuildReporter(_context.BuildLogger, _context.Options, _context.EnvironmentOptions);

                // Note: MSBuild only allows one build at a time in a process.
                foreach (var projectInstance in projectInstancesToRegenerate)
                {
                    Logger.LogDebug("[{Project}] Regenerating scoped CSS bundle.", projectInstance.GetDisplayName());

                    using var loggers = buildReporter.GetLoggers(projectInstance.FullPath, "ScopedCss");

                    // Deep copy so that we don't pollute the project graph:
                    if (!projectInstance.DeepCopy().Build(s_targets, loggers))
                    {
                        loggers.ReportOutput();

                        failedApplicationProjectInstances ??= [];
                        failedApplicationProjectInstances.Add(projectInstance);
                    }
                }
            }

            // Creating apply tasks involves reading static assets from disk. Parallelize this IO.
            var applyTaskProducers = new List<Task<Task>>();

            foreach (var (applicationProjectInstance, instanceAssets) in assets)
            {
                if (failedApplicationProjectInstances?.Contains(applicationProjectInstance) == true)
                {
                    continue;
                }

                if (!TryGetRunningProject(applicationProjectInstance.FullPath, out var runningProjects))
                {
                    continue;
                }

                foreach (var runningProject in runningProjects)
                {
                    // Only cancel applying updates when the process exits. Canceling in-progress static asset update might be ok,
                    // but for consistency with managed code updates we only cancel when the process exits.
                    applyTaskProducers.Add(runningProject.Clients.ApplyStaticAssetUpdatesAsync(
                        instanceAssets.Values,
                        applyOperationCancellationToken: runningProject.ProcessExitedCancellationToken,
                        cancellationToken));
                }
            }

            var applyTasks = await Task.WhenAll(applyTaskProducers);

            // fire and forget:
            _ = CompleteApplyOperationAsync(applyTasks, stopwatch, MessageDescriptor.StaticAssetsChangesApplied);
        }

        /// <summary>
        /// Terminates all processes launched for non-root projects with <paramref name="projectPaths"/>,
        /// or all running non-root project processes if <paramref name="projectPaths"/> is null.
        ///
        /// Removes corresponding entries from <see cref="_runningProjects"/>.
        ///
        /// Does not terminate the root project.
        /// </summary>
        /// <returns>All processes (including root) to be restarted.</returns>
        internal async ValueTask<ImmutableArray<RunningProject>> TerminateNonRootProcessesAsync(
            IEnumerable<string>? projectPaths, CancellationToken cancellationToken)
        {
            ImmutableArray<RunningProject> projectsToRestart = [];

            lock (_runningProjectsAndUpdatesGuard)
            {
                projectsToRestart = projectPaths == null
                    ? [.. _runningProjects.SelectMany(entry => entry.Value)]
                    : [.. projectPaths.SelectMany(path => _runningProjects.TryGetValue(path, out var array) ? array : [])];
            }

            // Do not terminate root process at this time - it would signal the cancellation token we are currently using.
            // The process will be restarted later on.
            // Wait for all processes to exit to release their resources, so we can rebuild.
            await Task.WhenAll(projectsToRestart.Where(p => !p.Options.IsRootProject).Select(p => p.TerminateForRestartAsync())).WaitAsync(cancellationToken);

            return projectsToRestart;
        }

        private bool RemoveRunningProject(RunningProject project)
        {
            var projectPath = project.ProjectNode.ProjectInstance.FullPath;

            return UpdateRunningProjects(runningProjectsByPath =>
            {
                if (!runningProjectsByPath.TryGetValue(projectPath, out var runningInstances))
                {
                    return runningProjectsByPath;
                }

                var updatedRunningProjects = runningInstances.Remove(project);
                return updatedRunningProjects is []
                    ? runningProjectsByPath.Remove(projectPath)
                    : runningProjectsByPath.SetItem(projectPath, updatedRunningProjects);
            });
        }

        private bool UpdateRunningProjects(Func<ImmutableDictionary<string, ImmutableArray<RunningProject>>, ImmutableDictionary<string, ImmutableArray<RunningProject>>> updater)
        {
            lock (_runningProjectsAndUpdatesGuard)
            {
                var newRunningProjects = updater(_runningProjects);
                if (newRunningProjects != _runningProjects)
                {
                    _runningProjects = newRunningProjects;
                    return true;
                }

                return false;
            }
        }

        public bool TryGetRunningProject(string projectPath, out ImmutableArray<RunningProject> projects)
        {
            lock (_runningProjectsAndUpdatesGuard)
            {
                return _runningProjects.TryGetValue(projectPath, out projects);
            }
        }

        private static Task ForEachProjectAsync(ImmutableDictionary<string, ImmutableArray<RunningProject>> projects, Func<RunningProject, CancellationToken, Task> action, CancellationToken cancellationToken)
            => Task.WhenAll(projects.SelectMany(entry => entry.Value).Select(project => action(project, cancellationToken))).WaitAsync(cancellationToken);

        private static ImmutableArray<HotReloadManagedCodeUpdate> ToManagedCodeUpdates(ImmutableArray<HotReloadService.Update> updates)
            => [.. updates.Select(update => new HotReloadManagedCodeUpdate(update.ModuleId, update.MetadataDelta, update.ILDelta, update.PdbDelta, update.UpdatedTypes, update.RequiredCapabilities))];

        private static ImmutableDictionary<string, ImmutableArray<ProjectInstance>> CreateProjectInstanceMap(ProjectGraph graph)
            => graph.ProjectNodes
                .GroupBy(static node => node.ProjectInstance.FullPath)
                .ToImmutableDictionary(
                    keySelector: static group => group.Key,
                    elementSelector: static group => group.Select(static node => node.ProjectInstance).ToImmutableArray());

        public async Task UpdateProjectGraphAsync(ProjectGraph projectGraph, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Loading projects ...");
            var stopwatch = Stopwatch.StartNew();

            _projectInstances = CreateProjectInstanceMap(projectGraph);

            var solution = await Workspace.UpdateProjectGraphAsync([.. projectGraph.EntryPointNodes.Select(n => n.ProjectInstance.FullPath)], cancellationToken);
            await SolutionUpdatedAsync(solution, "project update", cancellationToken);

            Logger.LogInformation("Projects loaded in {Time}s.", stopwatch.Elapsed.TotalSeconds.ToString("0.0"));
        }

        public async Task UpdateFileContentAsync(IReadOnlyList<ChangedFile> changedFiles, CancellationToken cancellationToken)
        {
            var solution = await Workspace.UpdateFileContentAsync(changedFiles.Select(static f => (f.Item.FilePath, f.Kind.Convert())), cancellationToken);
            await SolutionUpdatedAsync(solution, "document update", cancellationToken);
        }

        private Task SolutionUpdatedAsync(Solution newSolution, string operationDisplayName, CancellationToken cancellationToken)
            => ReportSolutionFilesAsync(newSolution, Interlocked.Increment(ref _solutionUpdateId), operationDisplayName, cancellationToken);

        private async Task ReportSolutionFilesAsync(Solution solution, int updateId, string operationDisplayName, CancellationToken cancellationToken)
        {
            Logger.LogDebug("Solution after {Operation}: v{Version}", operationDisplayName, updateId);

            if (!Logger.IsEnabled(LogLevel.Trace))
            {
                return;
            }

            foreach (var project in solution.Projects)
            {
                Logger.LogDebug("  Project: {Path}", project.FilePath);

                foreach (var document in project.Documents)
                {
                    await InspectDocumentAsync(document, "Document").ConfigureAwait(false);
                }

                foreach (var document in project.AdditionalDocuments)
                {
                    await InspectDocumentAsync(document, "Additional").ConfigureAwait(false);
                }

                foreach (var document in project.AnalyzerConfigDocuments)
                {
                    await InspectDocumentAsync(document, "Config").ConfigureAwait(false);
                }
            }

            async ValueTask InspectDocumentAsync(TextDocument document, string kind)
            {
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                Logger.LogDebug("    {Kind}: {FilePath} [{Checksum}]", kind, document.FilePath, Convert.ToBase64String(text.GetChecksum().ToArray()));
            }
        }
    }
}
