// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Xml.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Graph;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.HotReload;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch
{
    internal sealed class HotReloadDotNetWatcher
    {
        public const string ClientLogComponentName = $"{nameof(HotReloadDotNetWatcher)}:Client";
        public const string AgentLogComponentName = $"{nameof(HotReloadDotNetWatcher)}:Agent";

        private readonly IConsole _console;
        private readonly IRuntimeProcessLauncherFactory? _runtimeProcessLauncherFactory;
        private readonly RestartPrompt? _rudeEditRestartPrompt;

        private readonly DotNetWatchContext _context;
        private readonly ProjectGraphFactory _designTimeBuildGraphFactory = null!;

        private volatile CancellationTokenSource? _forceRestartCancellationSource;

        internal Task? Test_FileChangesCompletedTask { get; set; }

        public HotReloadDotNetWatcher(DotNetWatchContext context, IConsole console, IRuntimeProcessLauncherFactory? runtimeProcessLauncherFactory)
        {
            _context = context;
            _console = console;
            _runtimeProcessLauncherFactory = runtimeProcessLauncherFactory;
            if (!context.Options.NonInteractive)
            {
                var consoleInput = new ConsoleInputReader(_console, context.Options.LogLevel, context.EnvironmentOptions.SuppressEmojis);

                var noPrompt = context.EnvironmentOptions.RestartOnRudeEdit;
                if (noPrompt)
                {
                    context.Logger.LogDebug("DOTNET_WATCH_RESTART_ON_RUDE_EDIT = 'true'. Will restart without prompt.");
                }

                _rudeEditRestartPrompt = new RestartPrompt(context.Logger, consoleInput, noPrompt ? true : null);
            }

            _designTimeBuildGraphFactory = new ProjectGraphFactory(
                _context.RootProjects,
                _context.TargetFramework,
                globalOptions: EvaluationResult.GetGlobalBuildOptions(
                    context.BuildArguments,
                    context.EnvironmentOptions));
        }

        internal void RequestRestart()
        {
            _forceRestartCancellationSource?.Cancel();
        }

        public async Task WatchAsync(CancellationToken shutdownCancellationToken)
        {
            _context.Logger.Log(MessageDescriptor.HotReloadEnabled);
            _context.Logger.Log(MessageDescriptor.PressCtrlRToRestart);

            _console.KeyPressed += (key) =>
            {
                if (key.Modifiers.HasFlag(ConsoleModifiers.Control) && key.Key == ConsoleKey.R && _forceRestartCancellationSource is { } source)
                {
                    // provide immediate feedback to the user:
                    _context.Logger.Log(source.IsCancellationRequested ? MessageDescriptor.RestartInProgress : MessageDescriptor.RestartRequested);
                    source.Cancel();
                }
            };

            using var fileWatcher = new FileWatcher(_context.Logger, _context.EnvironmentOptions);

            for (var iteration = 0; !shutdownCancellationToken.IsCancellationRequested; iteration++)
            {
                Interlocked.Exchange(ref _forceRestartCancellationSource, new CancellationTokenSource())?.Dispose();

                using var rootProcessTerminationSource = new CancellationTokenSource();

                // This source will signal when the user cancels (either Ctrl+R or Ctrl+C) or when the root process terminates:
                using var iterationCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(shutdownCancellationToken, _forceRestartCancellationSource.Token, rootProcessTerminationSource.Token);
                var iterationCancellationToken = iterationCancellationSource.Token;

                var waitForFileChangeBeforeRestarting = true;
                EvaluationResult? evaluationResult = null;
                RunningProject? rootRunningProject = null;
                IRuntimeProcessLauncher? runtimeProcessLauncher = null;
                CompilationHandler? compilationHandler = null;
                Action<ChangedPath>? fileChangedCallback = null;

                try
                {
                    var rootProjectPaths = _context.RootProjects.Select(p => p.ProjectOrEntryPointFilePath);
                    await EmitStatusEventAsync(WatchStatusEvent.Types.Building, rootProjectPaths);

                    var buildSucceeded = await BuildProjectsAsync(_context.RootProjects, _context.BuildArguments, iterationCancellationToken);

                    await EmitStatusEventAsync(WatchStatusEvent.Types.BuildComplete, rootProjectPaths, success: buildSucceeded);

                    if (!buildSucceeded)
                    {
                        continue;
                    }

                    // Evaluate the target to find out the set of files to watch.
                    // In case the app fails to start due to build or other error we can wait for these files to change.
                    // Avoid restore since the build above already restored the root project.
                    evaluationResult = await EvaluateProjectGraphAsync(restore: false, iterationCancellationToken);

                    var projectMap = new ProjectNodeMap(evaluationResult.ProjectGraph, _context.Logger);
                    compilationHandler = new CompilationHandler(_context);
                    var projectLauncher = new ProjectLauncher(_context, projectMap, compilationHandler, iteration);
                    evaluationResult.ItemExclusions.Report(_context.Logger);

                    var runtimeProcessLauncherFactory = _runtimeProcessLauncherFactory;

                    var rootProjectOptions = _context.RootProjectOptions;
                    var rootProject = (rootProjectOptions != null) ? evaluationResult.ProjectGraph.GraphRoots.Single() : null;

                    //var rootProjectCapabilities = rootProject.GetCapabilities();
                    //if (rootProjectCapabilities.Contains(AspireServiceFactory.AppHostProjectCapability))
                    //{
                    //    runtimeProcessLauncherFactory ??= AspireServiceFactory.Instance;
                    //    _context.Logger.LogDebug("Using Aspire process launcher.");
                    //}

                    runtimeProcessLauncher = runtimeProcessLauncherFactory?.Create(
                        projectLauncher,
                        launchProfile: _context.LaunchProfileName,
                        targetFramework: _context.TargetFramework,
                        buildArguments: _context.BuildArguments,
                        onLaunchedProcessCrashed: () =>
                        {
                            // Mirror the root process onExit behavior (line 154-159):
                            // cancel the iteration, wait for file change, then restart.
                            waitForFileChangeBeforeRestarting = true;
                            iterationCancellationSource.Cancel();
                        });

                    if (rootProjectOptions != null)
                    {
                        if (runtimeProcessLauncher != null)
                        {
                            rootProjectOptions = rootProjectOptions with
                            {
                                LaunchEnvironmentVariables = [.. rootProjectOptions.LaunchEnvironmentVariables, .. runtimeProcessLauncher.GetEnvironmentVariables()]
                            };
                        }

                        rootRunningProject = await projectLauncher.TryLaunchProcessAsync(
                            rootProjectOptions,
                            rootProcessTerminationSource,
                            onOutput: null,
                            onExit: (_, _) =>
                            {
                                // Process exited: cancel the iteration, but wait for a file change before starting a new one
                                waitForFileChangeBeforeRestarting = true;
                                iterationCancellationSource.Cancel();
                                return ValueTask.CompletedTask;
                            },
                            restartOperation: new RestartOperation(_ => default), // the process will automatically restart
                            iterationCancellationToken);

                        if (rootRunningProject == null)
                        {
                            // error has been reported:
                            waitForFileChangeBeforeRestarting = false;
                            return;
                        }

                        // Cancel iteration as soon as the root process exits, so that we don't spent time loading solution, etc. when the process is already dead.
                        rootRunningProject.ProcessExitedCancellationToken.Register(iterationCancellationSource.Cancel);

                        if (shutdownCancellationToken.IsCancellationRequested)
                        {
                            // Ctrl+C:
                            return;
                        }

                        if (!await rootRunningProject.WaitForProcessRunningAsync(iterationCancellationToken))
                        {
                            // Process might have exited while we were trying to communicate with it.
                            // Cancel the iteration, but wait for a file change before starting a new one.
                            iterationCancellationSource.Cancel();
                            iterationCancellationSource.Token.ThrowIfCancellationRequested();
                        }

                        if (shutdownCancellationToken.IsCancellationRequested)
                        {
                            // Ctrl+C:
                            return;
                        }
                    }

                    await compilationHandler.UpdateProjectGraphAsync(evaluationResult.ProjectGraph, iterationCancellationToken);

                    // Solution must be initialized after we load the solution but before we start watching for file changes to avoid race condition
                    // when the EnC session captures content of the file after the changes has already been made.
                    // The session must also start after the project is built, so that the EnC service can read document checksums from the PDB.
                    await compilationHandler.StartSessionAsync(iterationCancellationToken);

                    if (shutdownCancellationToken.IsCancellationRequested)
                    {
                        // Ctrl+C:
                        return;
                    }

                    evaluationResult.WatchFiles(fileWatcher);

                    var changedFilesAccumulator = ImmutableList<ChangedPath>.Empty;

                    void FileChangedCallback(ChangedPath change)
                    {
                        if (AcceptChange(change, evaluationResult))
                        {
                            _context.Logger.LogDebug("File change: {Kind} '{Path}'.", change.Kind, change.Path);
                            ImmutableInterlocked.Update(ref changedFilesAccumulator, changedPaths => changedPaths.Add(change));
                        }
                    }

                    fileChangedCallback = FileChangedCallback;
                    fileWatcher.OnFileChange += fileChangedCallback;
                    _context.Logger.Log(MessageDescriptor.WaitingForChanges);

                    // Hot Reload loop - exits when the root process needs to be restarted.
                    var extendTimeout = false;
                    while (true)
                    {
                        try
                        {
                            if (Test_FileChangesCompletedTask != null)
                            {
                                await Test_FileChangesCompletedTask;
                            }

                            // Use timeout to batch file changes. If the process doesn't exit within the given timespan we'll check
                            // for accumulated file changes. If there are any we attempt Hot Reload. Otherwise we come back here to wait again.
                            await Task.Delay(TimeSpan.FromMilliseconds(extendTimeout ? 200 : 50), iterationCancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            // Ctrl+C, forced restart, or process exited.
                            Debug.Assert(iterationCancellationToken.IsCancellationRequested);

                            // Will wait for a file change if process exited.
                            waitForFileChangeBeforeRestarting = true;
                            break;
                        }

                        // If the changes include addition/deletion wait a little bit more for possible matching deletion/addition.
                        // This eliminates reevaluations caused by teared add + delete of a temp file or a move of a file.
                        if (!extendTimeout && changedFilesAccumulator.Any(change => change.Kind is ChangeKind.Add or ChangeKind.Delete))
                        {
                            extendTimeout = true;
                            continue;
                        }

                        extendTimeout = false;

                        var changedFiles = await CaptureChangedFilesSnapshot(rebuiltProjects: []);
                        if (changedFiles is [])
                        {
                            continue;
                        }

                        HotReloadEventSource.Log.HotReloadStart(HotReloadEventSource.StartType.Main);
                        var stopwatch = Stopwatch.StartNew();

                        HotReloadEventSource.Log.HotReloadStart(HotReloadEventSource.StartType.StaticHandler);
                        await compilationHandler.HandleStaticAssetChangesAsync(changedFiles, projectMap, evaluationResult.StaticWebAssetsManifests, stopwatch, iterationCancellationToken);
                        HotReloadEventSource.Log.HotReloadEnd(HotReloadEventSource.StartType.StaticHandler);

                        HotReloadEventSource.Log.HotReloadStart(HotReloadEventSource.StartType.CompilationHandler);

                        var (managedCodeUpdates, projectsToRebuild, projectsToRedeploy, projectsToRestart) = await compilationHandler.HandleManagedCodeChangesAsync(
                            autoRestart: _context.Options.NonInteractive || _rudeEditRestartPrompt?.AutoRestartPreference is true,
                            restartPrompt: async (projectNames, cancellationToken) =>
                            {
                                if (_rudeEditRestartPrompt != null)
                                {
                                    // stop before waiting for user input:
                                    stopwatch.Stop();

                                    string question;
                                    if (runtimeProcessLauncher == null)
                                    {
                                        question = "Do you want to restart your app?";
                                    }
                                    else
                                    {
                                        _context.Logger.LogInformation("Affected projects:");

                                        foreach (var projectName in projectNames.OrderBy(n => n))
                                        {
                                            _context.Logger.LogInformation("  {ProjectName}", projectName);
                                        }

                                        question = "Do you want to restart these projects?";
                                    }

                                    return await _rudeEditRestartPrompt.WaitForRestartConfirmationAsync(question, cancellationToken);
                                }

                                _context.Logger.LogDebug("Restarting without prompt since dotnet-watch is running in non-interactive mode.");

                                foreach (var projectName in projectNames)
                                {
                                    _context.Logger.LogDebug("  Project to restart: '{ProjectName}'", projectName);
                                }

                                return true;
                            },
                            iterationCancellationToken);

                        HotReloadEventSource.Log.HotReloadEnd(HotReloadEventSource.StartType.CompilationHandler);

                        stopwatch.Stop();

                        HotReloadEventSource.Log.HotReloadEnd(HotReloadEventSource.StartType.Main);

                        // Terminate root process if it had rude edits or is non-reloadable.
                        if (projectsToRestart.SingleOrDefault(project => project.Options.IsRootProject) is { } rootProjectToRestart)
                        {
                            // Triggers rootRestartCancellationToken.
                            waitForFileChangeBeforeRestarting = false;
                            break;
                        }

                        if (!projectsToRebuild.IsEmpty)
                        {
                            while (true)
                            {
                                iterationCancellationToken.ThrowIfCancellationRequested();

                                await EmitStatusEventAsync(WatchStatusEvent.Types.Building, projectsToRebuild);

                                // pause accumulating file changes during build:
                                fileWatcher.SuppressEvents = true;
                                bool rebuildSuccess;
                                try
                                {
                                    rebuildSuccess = await BuildProjectsAsync([.. projectsToRebuild.Select(ProjectRepresentation.FromProjectOrEntryPointFilePath)], _context.BuildArguments, iterationCancellationToken);
                                }
                                finally
                                {
                                    fileWatcher.SuppressEvents = false;
                                }

                                await EmitStatusEventAsync(WatchStatusEvent.Types.BuildComplete, projectsToRebuild, success: rebuildSuccess);

                                if (rebuildSuccess)
                                {
                                    break;
                                }

                                iterationCancellationToken.ThrowIfCancellationRequested();

                                _ = await fileWatcher.WaitForFileChangeAsync(
                                    change => AcceptChange(change, evaluationResult),
                                    startedWatching: () => _context.Logger.Log(MessageDescriptor.FixBuildError),
                                    shutdownCancellationToken);
                            }

                            // Changes made since last snapshot of the accumulator shouldn't be included in next Hot Reload update.
                            // Apply them to the workspace.
                            _ = await CaptureChangedFilesSnapshot(projectsToRebuild);

                            _context.Logger.Log(MessageDescriptor.ProjectsRebuilt, projectsToRebuild.Length);
                        }

                        // Deploy dependencies after rebuilding and before restarting.
                        if (!projectsToRedeploy.IsEmpty)
                        {
                            DeployProjectDependencies(evaluationResult.RestoredProjectInstances, projectsToRedeploy, iterationCancellationToken);
                            _context.Logger.Log(MessageDescriptor.ProjectDependenciesDeployed, projectsToRedeploy.Length);
                        }

                        // Apply updates only after dependencies have been deployed,
                        // so that updated code doesn't attempt to access the dependency before it has been deployed.
                        if (!managedCodeUpdates.IsEmpty)
                        {
                            await compilationHandler.ApplyUpdatesAsync(managedCodeUpdates, stopwatch, iterationCancellationToken);
                            await EmitStatusEventAsync(WatchStatusEvent.Types.HotReloadApplied, changedFiles.SelectMany(f => f.Item.ContainingProjectPaths).Distinct());
                        }

                        if (!projectsToRestart.IsEmpty)
                        {
                            await EmitStatusEventAsync(WatchStatusEvent.Types.Restarting, projectsToRestart.Select(p => p.Options.Representation.ProjectOrEntryPointFilePath));

                            await Task.WhenAll(
                                projectsToRestart.Select(async runningProject =>
                                {
                                    var newRunningProject = await runningProject.RestartOperation(shutdownCancellationToken);
                                    _ = await newRunningProject.WaitForProcessRunningAsync(shutdownCancellationToken);
                                }))
                                .WaitAsync(shutdownCancellationToken);

                            // ProcessStarted event (emitted by ProcessLauncherFactory.StartProjectAsync)
                            // already set the state to Running. No additional signal needed here.

                            _context.Logger.Log(MessageDescriptor.ProjectsRestarted, projectsToRestart.Length);
                        }

                        async Task<ImmutableArray<ChangedFile>> CaptureChangedFilesSnapshot(ImmutableArray<string> rebuiltProjects)
                        {
                            var changedPaths = Interlocked.Exchange(ref changedFilesAccumulator, []);
                            if (changedPaths is [])
                            {
                                return [];
                            }

                            // Note:
                            // It is possible that we could have received multiple changes for a file that should cancel each other (such as Delete + Add),
                            // but they end up split into two snapshots and we will interpret them as two separate Delete and Add changes that trigger
                            // two sets of Hot Reload updates. Hence the normalization is best effort as we can't predict future.

                            var changedFiles = NormalizePathChanges(changedPaths)
                                .Select(changedPath =>
                                {
                                    // On macOS may report Update followed by Add when a new file is created or just updated.
                                    // We normalize Update + Add to just Add and Update + Add + Delete to Update above.
                                    // To distinguish between an addition and an update we check if the file exists.

                                    if (evaluationResult.Files.TryGetValue(changedPath.Path, out var existingFileItem))
                                    {
                                        var changeKind = changedPath.Kind == ChangeKind.Add ? ChangeKind.Update : changedPath.Kind;

                                        return new ChangedFile(existingFileItem, changeKind);
                                    }

                                    // Do not assume the change is an addition, even if the file doesn't exist in the evaluation result.
                                    // The file could have been deleted and Add + Delete sequence could have been normalized to Update.
                                    return new ChangedFile(
                                        new FileItem() { FilePath = changedPath.Path, ContainingProjectPaths = [] },
                                        changedPath.Kind);
                                })
                                .Where(change => change.Kind switch
                                {
                                    ChangeKind.Add or ChangeKind.Update => fileWatcher.IsRecentChange(change.Item.FilePath),
                                    ChangeKind.Delete => true,
                                    _ => throw new InvalidOperationException()
                                })
                                .ToList();

                            ReportFileChanges(changedFiles);

                            AnalyzeFileChanges(changedFiles, evaluationResult, out var evaluationRequired);

                            if (evaluationRequired)
                            {
                                // TODO: consider re-evaluating only affected projects instead of the whole graph.
                                evaluationResult = await EvaluateProjectGraphAsync(restore: true, iterationCancellationToken);

                                // additional files/directories may have been added:
                                evaluationResult.WatchFiles(fileWatcher);

                                await compilationHandler.UpdateProjectGraphAsync(evaluationResult.ProjectGraph, iterationCancellationToken);

                                if (shutdownCancellationToken.IsCancellationRequested)
                                {
                                    // Ctrl+C:
                                    return [];
                                }

                                // Update files in the change set with new evaluation info.
                                for (var i = 0; i < changedFiles.Count; i++)
                                {
                                    var file = changedFiles[i];
                                    if (evaluationResult.Files.TryGetValue(file.Item.FilePath, out var evaluatedFile))
                                    {
                                        changedFiles[i] = file with { Item = evaluatedFile };
                                    }
                                }

                                _context.Logger.Log(MessageDescriptor.ReEvaluationCompleted);
                            }

                            if (!rebuiltProjects.IsEmpty)
                            {
                                // Filter changed files down to those contained in projects being rebuilt.
                                // File changes that affect projects that are not being rebuilt will stay in the accumulator
                                // and be included in the next Hot Reload change set.
                                var rebuiltProjectPaths = rebuiltProjects.ToHashSet();

                                var newAccumulator = ImmutableList<ChangedPath>.Empty;
                                var newChangedFiles = new List<ChangedFile>();

                                foreach (var file in changedFiles)
                                {
                                    if (file.Item.ContainingProjectPaths.All(rebuiltProjectPaths.Contains))
                                    {
                                        newChangedFiles.Add(file);
                                    }
                                    else
                                    {
                                        newAccumulator = newAccumulator.Add(new ChangedPath(file.Item.FilePath, file.Kind));
                                    }
                                }

                                changedFiles = newChangedFiles;

                                ImmutableInterlocked.Update(ref changedFilesAccumulator, accumulator => accumulator.AddRange(newAccumulator));
                            }

                            if (!evaluationRequired)
                            {
                                // Update the workspace to reflect changes in the file content:.
                                // If the project was re-evaluated the Roslyn solution is already up to date.
                                await compilationHandler.UpdateFileContentAsync(changedFiles, iterationCancellationToken);
                            }

                            return [.. changedFiles];
                        }
                    }
                }
                catch (OperationCanceledException) when (!shutdownCancellationToken.IsCancellationRequested)
                {
                    // start next iteration unless shutdown is requested
                }
                catch (Exception) when ((waitForFileChangeBeforeRestarting = false) == true)
                {
                    // unreachable
                    throw new InvalidOperationException();
                }
                finally
                {
                    // stop watching file changes:
                    if (fileChangedCallback != null)
                    {
                        fileWatcher.OnFileChange -= fileChangedCallback;
                    }

                    if (runtimeProcessLauncher != null)
                    {
                        // Request cleanup of all processes created by the launcher before we terminate the root process.
                        // Non-cancellable - can only be aborted by forced Ctrl+C, which immediately kills the dotnet-watch process.
                        await runtimeProcessLauncher.TerminateLaunchedProcessesAsync(CancellationToken.None);
                    }

                    if (compilationHandler != null)
                    {
                        // Non-cancellable - can only be aborted by forced Ctrl+C, which immediately kills the dotnet-watch process.
                        await compilationHandler.TerminateNonRootProcessesAndDispose(CancellationToken.None);
                    }

                    if (rootRunningProject != null)
                    {
                        await rootRunningProject.TerminateAsync();
                    }

                    if (runtimeProcessLauncher != null)
                    {
                        // Only dispose the launcher on full shutdown (Ctrl+C).
                        // During iteration restarts (crash recovery, rebuild command, forced restart),
                        // keep it alive so the DCP resource command's pipe connection survives.
                        if (shutdownCancellationToken.IsCancellationRequested)
                        {
                            await runtimeProcessLauncher.DisposeAsync();
                        }
                    }

                    if (waitForFileChangeBeforeRestarting &&
                        !shutdownCancellationToken.IsCancellationRequested &&
                        !_forceRestartCancellationSource.IsCancellationRequested &&
                        rootRunningProject?.IsRestarting != true)
                    {
                        using var shutdownOrForcedRestartSource = CancellationTokenSource.CreateLinkedTokenSource(shutdownCancellationToken, _forceRestartCancellationSource.Token);
                        await WaitForFileChangeBeforeRestarting(fileWatcher, evaluationResult, shutdownOrForcedRestartSource.Token);
                    }
                }
            }
        }

        private void AnalyzeFileChanges(
            List<ChangedFile> changedFiles,
            EvaluationResult evaluationResult,
            out bool evaluationRequired)
        {
            // If any build file changed (project, props, targets) we need to re-evaluate the projects.
            // Currently we re-evaluate the whole project graph even if only a single project file changed.
            if (changedFiles.Select(f => f.Item.FilePath).FirstOrDefault(path => evaluationResult.BuildFiles.Contains(path) || MatchesBuildFile(path)) is { } firstBuildFilePath)
            {
                _context.Logger.Log(MessageDescriptor.ProjectChangeTriggeredReEvaluation, firstBuildFilePath);
                evaluationRequired = true;
                return;
            }

            for (var i = 0; i < changedFiles.Count; i++)
            {
                var changedFile = changedFiles[i];
                var filePath = changedFile.Item.FilePath;

                if (changedFile.Kind is ChangeKind.Add)
                {
                    if (MatchesStaticWebAssetFilePattern(evaluationResult, filePath, out var staticWebAssetUrl))
                    {
                        changedFiles[i] = changedFile with
                        {
                            Item = changedFile.Item with { StaticWebAssetRelativeUrl = staticWebAssetUrl }
                        };
                    }
                    else
                    {
                        // TODO: https://github.com/dotnet/sdk/issues/52390
                        // Get patterns from evaluation that match Compile, AdditionalFile, AnalyzerConfigFile items.
                        // Avoid re-evaluating on addition of files that don't affect the project.

                        // project file or other file:
                        _context.Logger.Log(MessageDescriptor.FileAdditionTriggeredReEvaluation, filePath);
                        evaluationRequired = true;
                        return;
                    }
                }
            }

            evaluationRequired = false;
        }

        /// <summary>
        /// True if the file path looks like a file that might be imported by MSBuild.
        /// </summary>
        private static bool MatchesBuildFile(string filePath)
        {
            var extension = Path.GetExtension(filePath);
            return extension.Equals(".props", PathUtilities.OSSpecificPathComparison)
                || extension.Equals(".targets", PathUtilities.OSSpecificPathComparison)
                || extension.EndsWith("proj", PathUtilities.OSSpecificPathComparison)
                || extension.Equals("projitems", PathUtilities.OSSpecificPathComparison) // shared project items
                || string.Equals(Path.GetFileName(filePath), "global.json", PathUtilities.OSSpecificPathComparison);
        }

        /// <summary>
        /// Determines if the given file path is a static web asset file path based on
        /// the discovery patterns.
        /// </summary>
        private static bool MatchesStaticWebAssetFilePattern(EvaluationResult evaluationResult, string filePath, out string? staticWebAssetUrl)
        {
            staticWebAssetUrl = null;

            if (StaticWebAsset.IsScopedCssFile(filePath))
            {
                return true;
            }

            foreach (var (_, manifest) in evaluationResult.StaticWebAssetsManifests)
            {
                foreach (var pattern in manifest.DiscoveryPatterns)
                {
                    var match = pattern.Glob.MatchInfo(filePath);
                    if (match.IsMatch)
                    {
                        var dirUrl = match.WildcardDirectoryPartMatchGroup.Replace(Path.DirectorySeparatorChar, '/');

                        Debug.Assert(!dirUrl.EndsWith('/'));
                        Debug.Assert(!pattern.BaseUrl.EndsWith('/'));

                        var url = UrlEncoder.Default.Encode(dirUrl + "/" + match.FilenamePartMatchGroup);
                        if (pattern.BaseUrl != "")
                        {
                            url = pattern.BaseUrl + "/" + url;
                        }

                        staticWebAssetUrl = url;
                        return true;
                    }
                }
            }

            return false;
        }

        private void DeployProjectDependencies(ImmutableArray<ProjectInstance> restoredProjectInstances, ImmutableArray<string> projectPaths, CancellationToken cancellationToken)
        {
            var projectPathSet = projectPaths.ToImmutableHashSet(PathUtilities.OSSpecificPathComparer);
            var buildReporter = new BuildReporter(_context.Logger, _context.Options, _context.EnvironmentOptions);
            var targetName = TargetNames.ReferenceCopyLocalPathsOutputGroup;

            foreach (var restoredProjectInstance in restoredProjectInstances)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Avoid modification of the restored snapshot.
                var projectInstance = restoredProjectInstance.DeepCopy();

                var projectPath = projectInstance.FullPath;

                if (!projectPathSet.Contains(projectPath))
                {
                    continue;
                }

                if (!projectInstance.Targets.ContainsKey(targetName))
                {
                    continue;
                }

                if (projectInstance.GetOutputDirectory() is not { } relativeOutputDir)
                {
                    continue;
                }

                using var loggers = buildReporter.GetLoggers(projectPath, targetName);
                if (!projectInstance.Build([targetName], loggers, out var targetOutputs))
                {
                    _context.Logger.LogDebug("{TargetName} target failed", targetName);
                    loggers.ReportOutput();
                    continue;
                }

                var outputDir = Path.Combine(Path.GetDirectoryName(projectPath)!, relativeOutputDir);

                foreach (var item in targetOutputs[targetName].Items)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var sourcePath = item.ItemSpec;
                    var targetPath = Path.Combine(outputDir, item.GetMetadata(MetadataNames.TargetPath));
                    if (!File.Exists(targetPath))
                    {
                        _context.Logger.LogDebug("Deploying project dependency '{TargetPath}' from '{SourcePath}'", targetPath, sourcePath);

                        try
                        {
                            var directory = Path.GetDirectoryName(targetPath);
                            if (directory != null)
                            {
                                Directory.CreateDirectory(directory);
                            }

                            File.Copy(sourcePath, targetPath, overwrite: false);
                        }
                        catch (Exception e)
                        {
                            _context.Logger.LogDebug("Copy failed: {Message}", e.Message);
                        }
                    }
                }
            }
        }

        private async ValueTask WaitForFileChangeBeforeRestarting(FileWatcher fileWatcher, EvaluationResult? evaluationResult, CancellationToken cancellationToken)
        {
            if (evaluationResult != null)
            {
                if (!fileWatcher.WatchingDirectories)
                {
                    evaluationResult.WatchFiles(fileWatcher);
                }

                _ = await fileWatcher.WaitForFileChangeAsync(
                    evaluationResult.Files,
                    startedWatching: () => _context.Logger.Log(MessageDescriptor.WaitingForFileChangeBeforeRestarting),
                    cancellationToken);
            }
            else
            {
                // evaluation cancelled - watch for any changes in the directory tree containing the root project or entry-point file:
                fileWatcher.WatchContainingDirectories(_context.RootProjects.Select(p => p.ProjectOrEntryPointFilePath), includeSubdirectories: true);

                _ = await fileWatcher.WaitForFileChangeAsync(
                    acceptChange: AcceptChange,
                    startedWatching: () => _context.Logger.Log(MessageDescriptor.WaitingForFileChangeBeforeRestarting),
                    cancellationToken);
            }
        }

        private bool AcceptChange(ChangedPath change, EvaluationResult evaluationResult)
        {
            var (path, kind) = change;

            // Handle changes to files that are known to be project build inputs from its evaluation.
            // Compile items might be explicitly added by targets to directories that are excluded by default
            // (e.g. global usings in obj directory). Changes to these files should not be ignored.
            if (evaluationResult.Files.ContainsKey(path))
            {
                return true;
            }

            if (!AcceptChange(change))
            {
                return false;
            }

            // changes in *.*proj, *.props, *.targets:
            if (evaluationResult.BuildFiles.Contains(path))
            {
                return true;
            }

            // Ignore other changes that match DefaultItemExcludes glob if EnableDefaultItems is true,
            // otherwise changes under output and intermediate output directories.
            //
            // Unsupported scenario:
            // - msbuild target adds source files to intermediate output directory and Compile items
            //   based on the content of non-source file.
            //
            // On the other hand, changes to source files produced by source generators will be registered
            // since the changes to additional file will trigger workspace update, which will trigger the source generator.
            return !evaluationResult.ItemExclusions.IsExcluded(path, kind, _context.Logger);
        }

        private bool AcceptChange(ChangedPath change)
        {
            var (path, kind) = change;

            if (Path.GetExtension(path) == ".binlog")
            {
                return false;
            }

            if (PathUtilities.GetContainingDirectories(path).FirstOrDefault(IsHiddenDirectory) is { } containingHiddenDir)
            {
                _context.Logger.Log(MessageDescriptor.IgnoringChangeInHiddenDirectory, containingHiddenDir, kind, path);
                return false;
            }

            return true;
        }

        // Directory name starts with '.' on Unix is considered hidden.
        // Apply the same convention on Windows as well (instead of checking for hidden attribute).
        // This is consistent with SDK rules for default item exclusions:
        // https://github.com/dotnet/sdk/blob/124be385f90f2c305dde2b817cb470e4d11d2d6b/src/Tasks/Microsoft.NET.Build.Tasks/targets/Microsoft.NET.Sdk.DefaultItems.targets#L42
        private static bool IsHiddenDirectory(string dir)
            => Path.GetFileName(dir).StartsWith('.');

        internal static IEnumerable<ChangedPath> NormalizePathChanges(IEnumerable<ChangedPath> changes)
            => changes
                .GroupBy(keySelector: change => change.Path)
                .Select(group =>
                {
                    ChangedPath? lastUpdate = null;
                    ChangedPath? lastDelete = null;
                    ChangedPath? lastAdd = null;
                    ChangedPath? previous = null;

                    foreach (var item in group)
                    {
                        // eliminate repeated changes:
                        if (item.Kind == previous?.Kind)
                        {
                            continue;
                        }

                        previous = item;

                        if (item.Kind == ChangeKind.Add)
                        {
                            // eliminate delete-(update)*-add:
                            if (lastDelete.HasValue)
                            {
                                lastDelete = null;
                                lastAdd = null;
                                lastUpdate ??= item with { Kind = ChangeKind.Update };
                            }
                            else
                            {
                                lastAdd = item;
                            }
                        }
                        else if (item.Kind == ChangeKind.Delete)
                        {
                            // eliminate add-delete:
                            if (lastAdd.HasValue)
                            {
                                lastDelete = null;
                                lastAdd = null;
                            }
                            else
                            {
                                lastDelete = item;

                                // eliminate previous update:
                                lastUpdate = null;
                            }
                        }
                        else if (item.Kind == ChangeKind.Update)
                        {
                            // ignore updates after add:
                            if (!lastAdd.HasValue)
                            {
                                lastUpdate = item;
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException($"Unexpected change kind: {item.Kind}");
                        }
                    }

                    return lastDelete ?? lastAdd ?? lastUpdate;
                })
                .Where(item => item != null)
                .Select(item => item!.Value);

        private void ReportFileChanges(IReadOnlyList<ChangedFile> changedFiles)
        {
            Report(kind: ChangeKind.Add);
            Report(kind: ChangeKind.Update);
            Report(kind: ChangeKind.Delete);

            void Report(ChangeKind kind)
            {
                var items = changedFiles.Where(item => item.Kind == kind).ToArray();
                if (items is not [])
                {
                    _context.Logger.LogInformation(GetMessage(items, kind));
                }
            }

            string GetMessage(IReadOnlyList<ChangedFile> items, ChangeKind kind)
                => items is [{Item: var item }]
                    ? GetSingularMessage(kind) + ": " + GetRelativeFilePath(item.FilePath)
                    : GetPluralMessage(kind) + ": " + string.Join(", ", items.Select(f => GetRelativeFilePath(f.Item.FilePath)));

            static string GetSingularMessage(ChangeKind kind)
                => kind switch
                {
                    ChangeKind.Update => "File updated",
                    ChangeKind.Add => "File added",
                    ChangeKind.Delete => "File deleted",
                    _ => throw new InvalidOperationException()
                };

            static string GetPluralMessage(ChangeKind kind)
                => kind switch
                {
                    ChangeKind.Update => "Files updated",
                    ChangeKind.Add => "Files added",
                    ChangeKind.Delete => "Files deleted",
                    _ => throw new InvalidOperationException()
                };
        }

        private async ValueTask<EvaluationResult> EvaluateProjectGraphAsync(bool restore, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _context.Logger.LogInformation("Evaluating projects ...");
                var stopwatch = Stopwatch.StartNew();

                var result = EvaluationResult.TryCreate(
                    _designTimeBuildGraphFactory,
                    _context.BuildLogger,
                    _context.Options,
                    _context.EnvironmentOptions,
                    restore,
                    cancellationToken);

                _context.Logger.LogInformation("Evaluation completed in {Time}s.", stopwatch.Elapsed.TotalSeconds.ToString("0.0"));

                if (result != null)
                {
                    return result;
                }

                await FileWatcher.WaitForFileChangeAsync(
                    _context.RootProjects.Select(p => p.ProjectOrEntryPointFilePath),
                    _context.Logger,
                    _context.EnvironmentOptions,
                    startedWatching: () => _context.Logger.Log(MessageDescriptor.FixBuildError),
                    cancellationToken);
            }
        }

        private async Task<bool> BuildProjectsAsync(ImmutableArray<ProjectRepresentation> projects, IReadOnlyList<string> buildArguments, CancellationToken cancellationToken)
        {
            List<OutputLine>? capturedOutput = _context.EnvironmentOptions.TestFlags != TestFlags.None ? [] : null;
            string? solutionFile = null;
            try
            {
                // TODO: workaround for https://github.com/dotnet/sdk/issues/51311
                // does not work with single-file apps
                if (projects is not [var project])
                {
                    solutionFile = Path.Combine(Path.GetTempFileName() + ".slnx");

                    var solutionElement = new XElement("Solution");

                    foreach (var p in projects)
                    {
                        if (p.PhysicalPath != null)
                        {
                            solutionElement.Add(new XElement("Project", new XAttribute("Path", p.PhysicalPath)));
                        }
                    }

                    var doc = new XDocument(solutionElement);
                    doc.Save(solutionFile);

                    project = new ProjectRepresentation(projectPath: solutionFile, entryPointFilePath: null);
                }

                var processSpec = new ProcessSpec
                {
                    Executable = _context.EnvironmentOptions.MuxerPath,
                    WorkingDirectory = project.GetContainingDirectory(),
                    IsUserApplication = false,

                    // Capture output if running in a test environment.
                    // If the output is not captured dotnet build will show live build progress.
                    OnOutput = capturedOutput != null
                        ? line =>
                        {
                            lock (capturedOutput)
                            {
                                capturedOutput.Add(line);
                            }
                        }
                    : null,

                    // pass user-specified build arguments last to override defaults:
                    Arguments = ["build", project.ProjectOrEntryPointFilePath, .. buildArguments]
                };

                _context.BuildLogger.Log(MessageDescriptor.Building, project.ProjectOrEntryPointFilePath);

                var success = await _context.ProcessRunner.RunAsync(processSpec, _context.Logger, launchResult: null, cancellationToken) == 0;

                if (capturedOutput != null)
                {
                    _context.BuildLogger.Log(success ? MessageDescriptor.BuildSucceeded : MessageDescriptor.BuildFailed, project.ProjectOrEntryPointFilePath);
                    BuildOutput.ReportBuildOutput(_context.BuildLogger, capturedOutput, success);
                }

                return success;
            }
            finally
            {
                if (solutionFile != null)
                {
                    try
                    {
                        File.Delete(solutionFile);
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
        }

        private Task EmitStatusEventAsync(string type, IEnumerable<string> projectPaths, bool? success = null, string? error = null)
        {
            var projects = projectPaths.ToArray();
            _context.Logger.LogDebug("Status event: {Type} (success={Success}) for [{Projects}]",
                type, success, string.Join(", ", projects.Select(p => Path.GetFileNameWithoutExtension(p))));

            if (_context.StatusEventWriter is not { } writer)
            {
                return Task.CompletedTask;
            }

            return writer(new WatchStatusEvent
            {
                Type = type,
                Projects = projects,
                Success = success,
                Error = error,
            });
        }

        private string GetRelativeFilePath(string path)
        {
            var relativePath = path;
            var workingDirectory = _context.EnvironmentOptions.WorkingDirectory;
            if (path.StartsWith(workingDirectory, StringComparison.Ordinal) && path.Length > workingDirectory.Length)
            {
                relativePath = path.Substring(workingDirectory.Length);

                return $".{(relativePath.StartsWith(Path.DirectorySeparatorChar) ? string.Empty : Path.DirectorySeparatorChar)}{relativePath}";
            }

            return relativePath;
        }
    }
}
