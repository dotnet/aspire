// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.DotNet.HotReload;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch
{
    internal enum Emoji
    {
        Default = 0,

        Warning,
        Error,
        HotReload,
        Watch,
        Stop,
        Restart,
        Launch,
        Wait,
        Aspire,
        Browser,
        Agent,
        Build,
        Refresh,
        LightBulb,
    }

    internal static class Extensions
    {
        public static string ToDisplay(this Emoji emoji)
            => emoji switch
            {
                Emoji.Default => ":",
                Emoji.Warning => "⚠",
                Emoji.Error => "❌",
                Emoji.HotReload => "🔥",
                Emoji.Watch => "⌚",
                Emoji.Stop => "🛑",
                Emoji.Restart => "🔄",
                Emoji.Launch => "🚀",
                Emoji.Wait => "⏳",
                Emoji.Aspire => "⭐",
                Emoji.Browser => "🌐",
                Emoji.Agent => "🕵️",
                Emoji.Build => "🔨",
                Emoji.Refresh => "🔃",
                Emoji.LightBulb => "💡",
                _ => throw new InvalidOperationException()
            };

        public static string GetLogMessagePrefix(this Emoji emoji)
            => $"dotnet watch {emoji.ToDisplay()} ";

        public static void Log(this ILogger logger, MessageDescriptor descriptor, params object?[] args)
        {
            logger.Log(
                descriptor.Level,
                descriptor.Id,
                state: (descriptor, args),
                exception: null,
                formatter: static (state, _) => state.descriptor.GetMessage(state.args));
        }
    }

    internal sealed class LoggerFactory(IReporter reporter, LogLevel level) : ILoggerFactory
    {
        private sealed class Logger(IReporter reporter, LogLevel level, string categoryName) : ILogger
        {
            public bool IsEnabled(LogLevel logLevel)
                => logLevel >= level;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel))
                {
                    return;
                }

                var (name, display) = LoggingUtilities.ParseCategoryName(categoryName);
                var prefix = display != null ? $"[{display}] " : "";

                var descriptor = eventId.Id != 0 ? MessageDescriptor.GetDescriptor(eventId) : default;

                var emoji = logLevel switch
                {
                    _ when descriptor.Emoji != Emoji.Default => descriptor.Emoji,
                    LogLevel.Error => Emoji.Error,
                    LogLevel.Warning => Emoji.Warning,
                    _ when MessageDescriptor.ComponentEmojis.TryGetValue(name, out var componentEmoji) => componentEmoji,
                    _ => Emoji.Watch
                };

                reporter.Report(eventId, emoji, logLevel, prefix + formatter(state, exception));
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull
                => throw new NotImplementedException();
        }

        public void Dispose()
        {
        }

        public ILogger CreateLogger(string categoryName)
            => new Logger(reporter, level, categoryName);

        public void AddProvider(ILoggerProvider provider)
            => throw new NotImplementedException();
    }

    internal readonly record struct MessageDescriptor(string Format, Emoji Emoji, LogLevel Level, EventId Id)
    {
        private static int s_id;
        private static ImmutableDictionary<EventId, MessageDescriptor> s_descriptors = [];
        
        private static MessageDescriptor Create(string format, Emoji emoji, LogLevel level)
            // reserve event id 0 for ad-hoc messages
            => Create(new EventId(++s_id), format, emoji, level);

        private static MessageDescriptor Create(LogEvent logEvent, Emoji emoji)
            => Create(logEvent.Id, logEvent.Message, emoji, logEvent.Level);

        private static MessageDescriptor Create(EventId id, string format, Emoji emoji, LogLevel level)
        {
            var descriptor = new MessageDescriptor(format, emoji, level, id.Id);
            s_descriptors = s_descriptors.Add(id, descriptor);
            return descriptor;
        }

        public static MessageDescriptor GetDescriptor(EventId id)
            => s_descriptors[id];

        public string GetMessage(params object?[] args)
            => Id.Id == 0 ? Format : string.Format(Format, args);

        public MessageDescriptor WithLevelWhen(LogLevel level, bool condition)
            => condition && Level != level
                ? this with
                  {
                      Level = level,
                      Emoji = level switch
                      {
                          LogLevel.Error or LogLevel.Critical => Emoji.Error,
                          LogLevel.Warning => Emoji.Warning,
                          _ => Emoji
                      }
                  }
                : this;

        public static readonly ImmutableDictionary<string, Emoji> ComponentEmojis = ImmutableDictionary<string, Emoji>.Empty
            .Add(DotNetWatchContext.DefaultLogComponentName, Emoji.Watch)
            .Add(DotNetWatchContext.BuildLogComponentName, Emoji.Build)
            .Add(HotReloadDotNetWatcher.ClientLogComponentName, Emoji.HotReload)
            .Add(HotReloadDotNetWatcher.AgentLogComponentName, Emoji.Agent)
            .Add(BrowserRefreshServer.ServerLogComponentName, Emoji.Refresh)
            .Add(BrowserConnection.AgentLogComponentName, Emoji.Agent)
            .Add(BrowserConnection.ServerLogComponentName, Emoji.Browser)
            .Add(AspireServiceFactory.AspireLogComponentName, Emoji.Aspire);

        // predefined messages used for testing:
        public static readonly MessageDescriptor HotReloadSessionStarting = Create("Hot reload session starting.", Emoji.HotReload, LogLevel.None);
        public static readonly MessageDescriptor HotReloadSessionStarted = Create("Hot reload session started.", Emoji.HotReload, LogLevel.Debug);
        public static readonly MessageDescriptor ProjectsRebuilt = Create("Projects rebuilt ({0})", Emoji.HotReload, LogLevel.Debug);
        public static readonly MessageDescriptor ProjectsRestarted = Create("Projects restarted ({0})", Emoji.HotReload, LogLevel.Debug);
        public static readonly MessageDescriptor ProjectDependenciesDeployed = Create("Project dependencies deployed ({0})", Emoji.HotReload, LogLevel.Debug);
        public static readonly MessageDescriptor FixBuildError = Create("Fix the error to continue or press Ctrl+C to exit.", Emoji.Watch, LogLevel.Warning);
        public static readonly MessageDescriptor WaitingForChanges = Create("Waiting for changes", Emoji.Watch, LogLevel.Information);
        public static readonly MessageDescriptor LaunchedProcess = Create("Launched '{0}' with arguments '{1}': process id {2}", Emoji.Launch, LogLevel.Debug);
        public static readonly MessageDescriptor HotReloadChangeHandled = Create("Hot reload change handled in {0}ms.", Emoji.HotReload, LogLevel.Debug);
        public static readonly MessageDescriptor HotReloadSucceeded = Create(LogEvents.HotReloadSucceeded, Emoji.HotReload);
        public static readonly MessageDescriptor UpdatesApplied = Create(LogEvents.UpdatesApplied, Emoji.HotReload);
        public static readonly MessageDescriptor Capabilities = Create(LogEvents.Capabilities, Emoji.HotReload);
        public static readonly MessageDescriptor WaitingForFileChangeBeforeRestarting = Create("Waiting for a file to change before restarting ...", Emoji.Wait, LogLevel.Warning);
        public static readonly MessageDescriptor WatchingWithHotReload = Create("Watching with Hot Reload.", Emoji.Watch, LogLevel.Debug);
        public static readonly MessageDescriptor RestartInProgress = Create("Restart in progress.", Emoji.Restart, LogLevel.Information);
        public static readonly MessageDescriptor RestartRequested = Create("Restart requested.", Emoji.Restart, LogLevel.Information);
        public static readonly MessageDescriptor ShutdownRequested = Create("Shutdown requested. Press Ctrl+C again to force exit.", Emoji.Stop, LogLevel.Information);
        public static readonly MessageDescriptor ApplyUpdate_Error = Create("{0}{1}", Emoji.Error, LogLevel.Error);
        public static readonly MessageDescriptor ApplyUpdate_Warning = Create("{0}{1}", Emoji.Warning, LogLevel.Warning);
        public static readonly MessageDescriptor ApplyUpdate_Verbose = Create("{0}{1}", Emoji.Default, LogLevel.Debug);
        public static readonly MessageDescriptor ApplyUpdate_ChangingEntryPoint = Create("{0} Press \"Ctrl + R\" to restart.", Emoji.Warning, LogLevel.Warning);
        public static readonly MessageDescriptor ApplyUpdate_FileContentDoesNotMatchBuiltSource = Create("{0} Expected if a source file is updated that is linked to project whose build is not up-to-date.", Emoji.Watch, LogLevel.Debug);
        public static readonly MessageDescriptor ConfiguredToLaunchBrowser = Create("dotnet-watch is configured to launch a browser on ASP.NET Core application startup.", Emoji.Watch, LogLevel.Debug);
        public static readonly MessageDescriptor ConfiguredToUseBrowserRefresh = Create("Using browser-refresh middleware", Emoji.Default, LogLevel.Debug);
        public static readonly MessageDescriptor SkippingConfiguringBrowserRefresh_SuppressedViaEnvironmentVariable = Create("Skipping configuring browser-refresh middleware since its refresh server suppressed via environment variable {0}.", Emoji.Watch, LogLevel.Debug);
        public static readonly MessageDescriptor SkippingConfiguringBrowserRefresh_TargetFrameworkNotSupported = Create("Skipping configuring browser-refresh middleware since the target framework version is not supported. For more information see 'https://aka.ms/dotnet/watch/unsupported-tfm'.", Emoji.Watch, LogLevel.Warning);
        public static readonly MessageDescriptor UpdatingDiagnostics = Create(LogEvents.UpdatingDiagnostics, Emoji.Default);
        public static readonly MessageDescriptor FailedToReceiveResponseFromConnectedBrowser = Create(LogEvents.FailedToReceiveResponseFromConnectedBrowser, Emoji.Default);
        public static readonly MessageDescriptor NoBrowserConnected = Create(LogEvents.NoBrowserConnected, Emoji.Default);
        public static readonly MessageDescriptor LaunchingBrowser = Create("Launching browser: {0} {1}", Emoji.Default, LogLevel.Debug);
        public static readonly MessageDescriptor RefreshingBrowser = Create(LogEvents.RefreshingBrowser, Emoji.Default);
        public static readonly MessageDescriptor ReloadingBrowser = Create(LogEvents.ReloadingBrowser, Emoji.Default);
        public static readonly MessageDescriptor RefreshServerRunningAt = Create(LogEvents.RefreshServerRunningAt, Emoji.Default);
        public static readonly MessageDescriptor ConnectedToRefreshServer = Create(LogEvents.ConnectedToRefreshServer, Emoji.Default);
        public static readonly MessageDescriptor RestartingApplicationToApplyChanges = Create("Restarting application to apply changes ...", Emoji.Default, LogLevel.Information);
        public static readonly MessageDescriptor RestartingApplication = Create("Restarting application ...", Emoji.Default, LogLevel.Information);
        public static readonly MessageDescriptor IgnoringChangeInHiddenDirectory = Create("Ignoring change in hidden directory '{0}': {1} '{2}'", Emoji.Watch, LogLevel.Trace);
        public static readonly MessageDescriptor IgnoringChangeInOutputDirectory = Create("Ignoring change in output directory: {0} '{1}'", Emoji.Watch, LogLevel.Trace);
        public static readonly MessageDescriptor IgnoringChangeInExcludedFile = Create("Ignoring change in excluded file '{0}': {1}. Path matches {2} glob '{3}' set in '{4}'.", Emoji.Watch, LogLevel.Trace);
        public static readonly MessageDescriptor FileAdditionTriggeredReEvaluation = Create("File addition triggered re-evaluation: '{0}'.", Emoji.Watch, LogLevel.Debug);
        public static readonly MessageDescriptor ProjectChangeTriggeredReEvaluation = Create("Project change triggered re-evaluation: '{0}'.", Emoji.Watch, LogLevel.Debug);
        public static readonly MessageDescriptor ReEvaluationCompleted = Create("Re-evaluation completed.", Emoji.Watch, LogLevel.Debug);
        public static readonly MessageDescriptor NoCSharpChangesToApply = Create("No C# changes to apply.", Emoji.Watch, LogLevel.Information);
        public static readonly MessageDescriptor Exited = Create("Exited", Emoji.Watch, LogLevel.Information);
        public static readonly MessageDescriptor ExitedWithUnknownErrorCode = Create("Exited with unknown error code", Emoji.Error, LogLevel.Error);
        public static readonly MessageDescriptor ExitedWithErrorCode = Create("Exited with error code {0}", Emoji.Error, LogLevel.Error);
        public static readonly MessageDescriptor FailedToLaunchProcess = Create("Failed to launch '{0}' with arguments '{1}': {2}", Emoji.Error, LogLevel.Error);
        public static readonly MessageDescriptor ApplicationFailed = Create("Application failed: {0}", Emoji.Error, LogLevel.Error);
        public static readonly MessageDescriptor ProcessRunAndExited = Create("Process id {0} ran for {1}ms and exited with exit code {2}.", Emoji.Watch, LogLevel.Debug);
        public static readonly MessageDescriptor WaitingForProcessToExitWithin = Create("Waiting for process {0} to exit within {1}s.", Emoji.Watch, LogLevel.Debug);
        public static readonly MessageDescriptor WaitingForProcessToExit = Create("Waiting for process {0} to exit ({1}).", Emoji.Watch, LogLevel.Debug);
        public static readonly MessageDescriptor FailedToKillProcess = Create("Failed to kill process {0}: {1}.", Emoji.Error, LogLevel.Error);
        public static readonly MessageDescriptor TerminatingProcess = Create("Terminating process {0} ({1}).", Emoji.Watch, LogLevel.Debug);
        public static readonly MessageDescriptor FailedToSendSignalToProcess = Create("Failed to send {0} signal to process {1}: {2}", Emoji.Warning, LogLevel.Warning);
        public static readonly MessageDescriptor ErrorReadingProcessOutput = Create("Error reading {0} of process {1}: {2}", Emoji.Watch, LogLevel.Debug);
        public static readonly MessageDescriptor StaticAssetsReloaded = Create("Static assets reloaded.", Emoji.HotReload, LogLevel.Information);
        public static readonly MessageDescriptor SendingStaticAssetUpdateRequest = Create(LogEvents.SendingStaticAssetUpdateRequest, Emoji.Default);
        public static readonly MessageDescriptor HotReloadCapabilities = Create("Hot reload capabilities: {0}.", Emoji.HotReload, LogLevel.Debug);
        public static readonly MessageDescriptor HotReloadSuspended = Create("Hot reload suspended. To continue hot reload, press \"Ctrl + R\".", Emoji.HotReload, LogLevel.Information);
        public static readonly MessageDescriptor UnableToApplyChanges = Create("Unable to apply changes due to compilation errors.", Emoji.HotReload, LogLevel.Information);
        public static readonly MessageDescriptor RestartNeededToApplyChanges = Create("Restart is needed to apply the changes.", Emoji.HotReload, LogLevel.Information);
        public static readonly MessageDescriptor HotReloadEnabled = Create("Hot reload enabled. For a list of supported edits, see https://aka.ms/dotnet/hot-reload.", Emoji.HotReload, LogLevel.Information);
        public static readonly MessageDescriptor PressCtrlRToRestart = Create("Press Ctrl+R to restart.", Emoji.LightBulb, LogLevel.Information);
        public static readonly MessageDescriptor HotReloadCanceledProcessExited = Create("Hot reload canceled because the process exited.", Emoji.HotReload, LogLevel.Debug);
        public static readonly MessageDescriptor ApplicationKind_BlazorHosted = Create("Application kind: BlazorHosted. '{0}' references BlazorWebAssembly project '{1}'.", Emoji.Default, LogLevel.Debug);
        public static readonly MessageDescriptor ApplicationKind_BlazorWebAssembly = Create("Application kind: BlazorWebAssembly.", Emoji.Default, LogLevel.Debug);
        public static readonly MessageDescriptor ApplicationKind_WebApplication = Create("Application kind: WebApplication.", Emoji.Default, LogLevel.Debug);
        public static readonly MessageDescriptor ApplicationKind_Default = Create("Application kind: Default.", Emoji.Default, LogLevel.Debug);
        public static readonly MessageDescriptor WatchingFilesForChanges = Create("Watching {0} file(s) for changes", Emoji.Watch, LogLevel.Debug);
        public static readonly MessageDescriptor WatchingFilesForChanges_FilePath = Create("> {0}", Emoji.Watch, LogLevel.Trace);
        public static readonly MessageDescriptor Building = Create("Building {0} ...", Emoji.Default, LogLevel.Information);
        public static readonly MessageDescriptor BuildSucceeded = Create("Build succeeded: {0}", Emoji.Default, LogLevel.Information);
        public static readonly MessageDescriptor BuildFailed = Create("Build failed: {0}", Emoji.Default, LogLevel.Information);
    }

    internal interface IProcessOutputReporter
    {
        /// <summary>
        /// If true, the output of the process will be prefixed with the project display name.
        /// Used for testing.
        /// </summary>
        bool PrefixProcessOutput { get; }

        /// <summary>
        /// Reports the output of a process that is being watched.
        /// </summary>
        /// <remarks>
        /// Not used to report output of dotnet-build processed launched by dotnet-watch to build or evaluate projects.
        /// </remarks>
        void ReportOutput(OutputLine line);
    }

    internal interface IReporter
    {
        void Report(EventId id, Emoji emoji, LogLevel level, string message);
    }
}
