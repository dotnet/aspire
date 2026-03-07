// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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

        public static string GetLogMessagePrefix(this Emoji emoji, string logMessagePrefix)
            => $"{logMessagePrefix} {emoji.ToDisplay()} ";

        public static void Log(this ILogger logger, MessageDescriptor<None> descriptor)
            => Log(logger, descriptor, default);

        public static void Log<TArgs>(this ILogger logger, MessageDescriptor<TArgs> descriptor, TArgs args)
        {
            logger.Log(
                descriptor.Level,
                descriptor.Id,
                state: new LogState<TArgs>(descriptor, args),
                exception: null,
                formatter: static (state, _) => state.Descriptor.GetMessage(state.Arguments));
        }

        public static void Log<TArg1, TArg2>(this ILogger logger, MessageDescriptor<(TArg1, TArg2)> descriptor, TArg1 arg1, TArg2 arg2)
            => Log(logger, descriptor, (arg1, arg2));

        public static void Log<TArg1, TArg2, TArg3>(this ILogger logger, MessageDescriptor<(TArg1, TArg2, TArg3)> descriptor, TArg1 arg1, TArg2 arg2, TArg3 arg3)
            => Log(logger, descriptor, (arg1, arg2, arg3));

        public static void Log<TArg1, TArg2, TArg3, TArg4>(this ILogger logger, MessageDescriptor<(TArg1, TArg2, TArg3, TArg4)> descriptor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
            => Log(logger, descriptor, (arg1, arg2, arg3, arg4));

        public static string GetMessage(this MessageDescriptor<None> descriptor)
            => descriptor.GetMessage(default);
    }

    internal readonly struct LogState<TArgs>(MessageDescriptor<TArgs> descriptor, TArgs arguments)
    {
        public MessageDescriptor<TArgs> Descriptor { get; } = descriptor;
        public TArgs Arguments { get; } = arguments;
    }

    internal sealed class LoggerFactory(IReporter reporter, LogLevel level) : ILoggerFactory
    {
        private sealed class Logger(IReporter reporter, LogLevel level, string categoryName) : ILogger
        {
            public bool IsEnabled(LogLevel logLevel)
                => logLevel >= level;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (logLevel == LogLevel.None || !IsEnabled(logLevel))
                {
                    return;
                }

                var (name, display) = LoggingUtilities.ParseCategoryName(categoryName);
                var prefix = display != null ? $"[{display}] " : "";

                var descriptor = eventId.Id != 0 ? MessageDescriptor.GetDescriptor(eventId) : null;

                var emoji = logLevel switch
                {
                    _ when descriptor != null && descriptor.Emoji != Emoji.Default => descriptor.Emoji,
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

    internal abstract class MessageDescriptor(string? format, Emoji emoji, LogLevel level, EventId id)
    {
        private static int s_id;
        private static ImmutableDictionary<EventId, MessageDescriptor> s_descriptors = [];

        public string? Format { get; } = format;
        public Emoji Emoji { get; } = emoji;
        public LogLevel Level { get; } = level;
        public EventId Id { get; } = id;

        private static MessageDescriptor<None> Create(string format, Emoji emoji, LogLevel level)
            => Create<None>(format, emoji, level);

        private static MessageDescriptor<TArgs> Create<TArgs>(string format, Emoji emoji, LogLevel level)
            // reserve event id 0 for ad-hoc messages
            => Create<TArgs>(new EventId(++s_id), format, emoji, level);

        private static MessageDescriptor<TArgs> Create<TArgs>(LogEvent<TArgs> logEvent, Emoji emoji)
            => Create<TArgs>(logEvent.Id, logEvent.Message, emoji, logEvent.Level);

        /// <summary>
        /// Creates a descriptor that's only used for notifications not displayed to the user.
        /// These can be used for testing or for custom loggers (e.g. Aspire status reporting).
        /// </summary>
        private static MessageDescriptor<TArgs> CreateNotification<TArgs>()
            => Create<TArgs>(new EventId(++s_id), format: null, Emoji.Default, LogLevel.None);

        private static MessageDescriptor<TArgs> Create<TArgs>(EventId id, string? format, Emoji emoji, LogLevel level)
        {
            var descriptor = new MessageDescriptor<TArgs>(format, emoji, level, id);
            s_descriptors = s_descriptors.Add(id, descriptor);
            return descriptor;
        }

        public static MessageDescriptor GetDescriptor(EventId id)
            => s_descriptors[id];

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
        public static readonly MessageDescriptor<string> CommandDoesNotSupportHotReload = Create<string>("Command '{0}' does not support Hot Reload.", Emoji.HotReload, LogLevel.Debug);
        public static readonly MessageDescriptor<None> HotReloadDisabledByCommandLineSwitch = Create("Hot Reload disabled by command line switch.", Emoji.HotReload, LogLevel.Debug);
        public static readonly MessageDescriptor<None> HotReloadSessionStartingNotification = CreateNotification<None>();
        public static readonly MessageDescriptor<None> HotReloadSessionStarted = Create("Hot reload session started.", Emoji.HotReload, LogLevel.Debug);
        public static readonly MessageDescriptor<int> ProjectsRebuilt = Create<int>("Projects rebuilt ({0})", Emoji.HotReload, LogLevel.Debug);
        public static readonly MessageDescriptor<int> ProjectsRestarted = Create<int>("Projects restarted ({0})", Emoji.HotReload, LogLevel.Debug);
        public static readonly MessageDescriptor<IEnumerable<ProjectRepresentation>> RestartingProjectsNotification = CreateNotification<IEnumerable<ProjectRepresentation>>();
        public static readonly MessageDescriptor<None> ProjectRestarting = Create("Restarting ...", Emoji.Watch, LogLevel.Debug);
        public static readonly MessageDescriptor<None> ProjectRestarted = Create("Restarted", Emoji.Watch, LogLevel.Debug);
        public static readonly MessageDescriptor<None> ProjectRelaunching = Create("Relaunching ...", Emoji.Watch, LogLevel.Information);
        public static readonly MessageDescriptor<None> ProjectRelaunched = Create("Relaunched", Emoji.Watch, LogLevel.Debug);
        public static readonly MessageDescriptor<None> ProcessCrashedAndWillBeRelaunched = Create("Process crashed and will be relaunched on file change", Emoji.Watch, LogLevel.Debug);
        public static readonly MessageDescriptor<int> ProjectDependenciesDeployed = Create<int>("Project dependencies deployed ({0})", Emoji.HotReload, LogLevel.Debug);
        public static readonly MessageDescriptor<None> FixBuildError = Create("Fix the error to continue or press Ctrl+C to exit.", Emoji.Watch, LogLevel.Warning);
        public static readonly MessageDescriptor<None> WaitingForChanges = Create("Waiting for changes", Emoji.Watch, LogLevel.Information);
        public static readonly MessageDescriptor<(string, string, int)> LaunchedProcess = Create<(string, string, int)>("Launched '{0}' with arguments '{1}': process id {2}", Emoji.Launch, LogLevel.Debug);
        public static readonly MessageDescriptor<long> ManagedCodeChangesApplied = Create<long>("C# and Razor changes applied in {0}ms.", Emoji.HotReload, LogLevel.Information);
        public static readonly MessageDescriptor<long> StaticAssetsChangesApplied = Create<long>("Static asset changes applied in {0}ms.", Emoji.HotReload, LogLevel.Information);
        public static readonly MessageDescriptor<IEnumerable<ProjectRepresentation>> ChangesAppliedToProjectsNotification = CreateNotification<IEnumerable<ProjectRepresentation>>();
        public static readonly MessageDescriptor<int> SendingUpdateBatch = Create(LogEvents.SendingUpdateBatch, Emoji.HotReload);
        public static readonly MessageDescriptor<int> UpdateBatchCompleted = Create(LogEvents.UpdateBatchCompleted, Emoji.HotReload);
        public static readonly MessageDescriptor<int> UpdateBatchFailed = Create(LogEvents.UpdateBatchFailed, Emoji.HotReload);
        public static readonly MessageDescriptor<int> UpdateBatchCanceled = Create(LogEvents.UpdateBatchCanceled, Emoji.HotReload);
        public static readonly MessageDescriptor<(int, string)> UpdateBatchFailedWithError = Create(LogEvents.UpdateBatchFailedWithError, Emoji.HotReload);
        public static readonly MessageDescriptor<(int, string)> UpdateBatchExceptionStackTrace = Create(LogEvents.UpdateBatchExceptionStackTrace, Emoji.HotReload);
        public static readonly MessageDescriptor<string> Capabilities = Create(LogEvents.Capabilities, Emoji.HotReload);
        public static readonly MessageDescriptor<None> WaitingForFileChangeBeforeRestarting = Create("Waiting for a file to change before restarting ...", Emoji.Wait, LogLevel.Warning);
        public static readonly MessageDescriptor<None> WatchingWithHotReload = Create("Watching with Hot Reload.", Emoji.Watch, LogLevel.Debug);
        public static readonly MessageDescriptor<None> RestartInProgress = Create("Restart in progress.", Emoji.Restart, LogLevel.Information);
        public static readonly MessageDescriptor<None> RestartRequested = Create("Restart requested.", Emoji.Restart, LogLevel.Information);
        public static readonly MessageDescriptor<None> Restarting = Create("Restarting.", Emoji.Restart, LogLevel.Information);
        public static readonly MessageDescriptor<None> ShutdownRequested = Create("Shutdown requested. Press Ctrl+C again to force exit.", Emoji.Stop, LogLevel.Information);
        public static readonly MessageDescriptor<string> ApplyUpdate_Error = Create<string>("{0}", Emoji.Error, LogLevel.Error);
        public static readonly MessageDescriptor<string> ApplyUpdate_Warning = Create<string>("{0}", Emoji.Warning, LogLevel.Warning);
        public static readonly MessageDescriptor<string> ApplyUpdate_Verbose = Create<string>("{0}", Emoji.Default, LogLevel.Debug);
        public static readonly MessageDescriptor<(string, string)> ApplyUpdate_AutoVerbose = Create<(string, string)>("{0}{1}", Emoji.Default, LogLevel.Debug);
        public static readonly MessageDescriptor<string> ApplyUpdate_ChangingEntryPoint = Create<string>("{0} Press \"Ctrl + R\" to restart.", Emoji.Warning, LogLevel.Warning);
        public static readonly MessageDescriptor<None> ConfiguredToLaunchBrowser = Create("dotnet-watch is configured to launch a browser on ASP.NET Core application startup.", Emoji.Watch, LogLevel.Debug);
        public static readonly MessageDescriptor<None> ConfiguredToUseBrowserRefresh = Create("Using browser-refresh middleware", Emoji.Default, LogLevel.Debug);
        public static readonly MessageDescriptor<string> BrowserRefreshSuppressedViaEnvironmentVariable_ManualRefreshRequired = Create<string>("Browser refresh is suppressed via environment variable '{0}'. To reload static assets after an update refresh browser manually.", Emoji.Watch, LogLevel.Debug);
        public static readonly MessageDescriptor<string> BrowserRefreshSuppressedViaEnvironmentVariable_ApplicationWillBeRestarted = Create<string>("Browser refresh is suppressed via environment variable '{0}'. Application will be restarted when updated.", Emoji.Watch, LogLevel.Warning);
        public static readonly MessageDescriptor<None> BrowserRefreshNotSupportedByProjectTargetFramework_ManualRefreshRequired = Create("Browser refresh is not supported by the project target framework. To reload static assets after an update refresh browser manually. For more information see 'https://aka.ms/dotnet/watch/unsupported-tfm'.", Emoji.Watch, LogLevel.Warning);
        public static readonly MessageDescriptor<None> BrowserRefreshNotSupportedByProjectTargetFramework_ApplicationWillBeRestarted = Create("Browser refresh is not supported by the project target framework. Application will be restarted when updated. For more information see 'https://aka.ms/dotnet/watch/unsupported-tfm'.", Emoji.Watch, LogLevel.Warning);
        public static readonly MessageDescriptor<None> UpdatingDiagnostics = Create(LogEvents.UpdatingDiagnostics, Emoji.Default);
        public static readonly MessageDescriptor<None> FailedToReceiveResponseFromConnectedBrowser = Create(LogEvents.FailedToReceiveResponseFromConnectedBrowser, Emoji.Default);
        public static readonly MessageDescriptor<None> NoBrowserConnected = Create(LogEvents.NoBrowserConnected, Emoji.Default);
        public static readonly MessageDescriptor<string> LaunchingBrowser = Create<string>("Launching browser: {0}", Emoji.Default, LogLevel.Debug);
        public static readonly MessageDescriptor<(string, string)> LaunchingBrowserWithUrl = Create<(string, string)>("Launching browser: {0} {1}", Emoji.Default, LogLevel.Debug);
        public static readonly MessageDescriptor<None> RefreshingBrowser = Create(LogEvents.RefreshingBrowser, Emoji.Default);
        public static readonly MessageDescriptor<None> ReloadingBrowser = Create(LogEvents.ReloadingBrowser, Emoji.Default);
        public static readonly MessageDescriptor<string> RefreshServerRunningAt = Create(LogEvents.RefreshServerRunningAt, Emoji.Default);
        public static readonly MessageDescriptor<None> ConnectedToRefreshServer = Create(LogEvents.ConnectedToRefreshServer, Emoji.Default);
        public static readonly MessageDescriptor<None> RestartingApplicationToApplyChanges = Create("Restarting application to apply changes ...", Emoji.Default, LogLevel.Information);
        public static readonly MessageDescriptor<None> RestartingApplication = Create("Restarting application ...", Emoji.Default, LogLevel.Information);
        public static readonly MessageDescriptor<(string, ChangeKind, string)> IgnoringChangeInHiddenDirectory = Create<(string, ChangeKind, string)>("Ignoring change in hidden directory '{0}': {1} '{2}'", Emoji.Watch, LogLevel.Trace);
        public static readonly MessageDescriptor<(ChangeKind, string)> IgnoringChangeInOutputDirectory = Create<(ChangeKind, string)>("Ignoring change in output directory: {0} '{1}'", Emoji.Watch, LogLevel.Trace);
        public static readonly MessageDescriptor<(string, ChangeKind, string, string, string)> IgnoringChangeInExcludedFile = Create<(string, ChangeKind, string, string, string)>("Ignoring change in excluded file '{0}': {1}. Path matches {2} glob '{3}' set in '{4}'.", Emoji.Watch, LogLevel.Trace);
        public static readonly MessageDescriptor<string> FileAdditionTriggeredReEvaluation = Create<string>("File addition triggered re-evaluation: '{0}'.", Emoji.Watch, LogLevel.Debug);
        public static readonly MessageDescriptor<string> ProjectChangeTriggeredReEvaluation = Create<string>("Project change triggered re-evaluation: '{0}'.", Emoji.Watch, LogLevel.Debug);
        public static readonly MessageDescriptor<None> ReEvaluationCompleted = Create("Re-evaluation completed.", Emoji.Watch, LogLevel.Debug);
        public static readonly MessageDescriptor<None> NoManagedCodeChangesToApply = Create("No managed code changes to apply.", Emoji.Watch, LogLevel.Information);
        public static readonly MessageDescriptor<None> Exited = Create("Exited", Emoji.Watch, LogLevel.Information);
        public static readonly MessageDescriptor<None> ExitedWithUnknownErrorCode = Create("Exited with unknown error code", Emoji.Error, LogLevel.Error);
        public static readonly MessageDescriptor<int> ExitedWithErrorCode = Create<int>("Exited with error code {0}", Emoji.Error, LogLevel.Error);
        public static readonly MessageDescriptor<(string, string, string)> FailedToLaunchProcess = Create<(string, string, string)>("Failed to launch '{0}' with arguments '{1}': {2}", Emoji.Error, LogLevel.Error);
        public static readonly MessageDescriptor<string> ApplicationFailed = Create<string>("Application failed: {0}", Emoji.Error, LogLevel.Error);
        public static readonly MessageDescriptor<(int, long, int?)> ProcessRunAndExited = Create<(int, long, int?)>("Process id {0} ran for {1}ms and exited with exit code {2}.", Emoji.Watch, LogLevel.Debug);
        public static readonly MessageDescriptor<(int, int)> WaitingForProcessToExitWithin = Create<(int, int)>("Waiting for process {0} to exit within {1}s.", Emoji.Watch, LogLevel.Debug);
        public static readonly MessageDescriptor<(int, int)> WaitingForProcessToExit = Create<(int, int)>("Waiting for process {0} to exit ({1}).", Emoji.Watch, LogLevel.Debug);
        public static readonly MessageDescriptor<(int, string)> FailedToKillProcess = Create<(int, string)>("Failed to kill process {0}: {1}.", Emoji.Error, LogLevel.Error);
        public static readonly MessageDescriptor<(int, string)> TerminatingProcess = Create<(int, string)>("Terminating process {0} ({1}).", Emoji.Watch, LogLevel.Debug);
        public static readonly MessageDescriptor<(string, int, string)> FailedToSendSignalToProcess = Create<(string, int, string)>("Failed to send {0} signal to process {1}: {2}", Emoji.Warning, LogLevel.Warning);
        public static readonly MessageDescriptor<(string, int, string)> ErrorReadingProcessOutput = Create<(string, int, string)>("Error reading {0} of process {1}: {2}", Emoji.Watch, LogLevel.Debug);
        public static readonly MessageDescriptor<string> SendingStaticAssetUpdateRequest = Create(LogEvents.SendingStaticAssetUpdateRequest, Emoji.Default);
        public static readonly MessageDescriptor<string> HotReloadCapabilities = Create<string>("Hot reload capabilities: {0}.", Emoji.HotReload, LogLevel.Debug);
        public static readonly MessageDescriptor<None> HotReloadSuspended = Create("Hot reload suspended. To continue hot reload, press \"Ctrl + R\".", Emoji.HotReload, LogLevel.Information);
        public static readonly MessageDescriptor<None> UnableToApplyChanges = Create("Unable to apply changes due to compilation errors.", Emoji.HotReload, LogLevel.Information);
        public static readonly MessageDescriptor<None> RestartNeededToApplyChanges = Create("Restart is needed to apply the changes.", Emoji.HotReload, LogLevel.Information);
        public static readonly MessageDescriptor<None> HotReloadEnabled = Create("Hot reload enabled. For a list of supported edits, see https://aka.ms/dotnet/hot-reload.", Emoji.HotReload, LogLevel.Information);
        public static readonly MessageDescriptor<string> ProjectDoesNotSupportHotReload = Create<string>("Project does not support Hot Reload: {0}. Application will be restarted when updated.", Emoji.Warning, LogLevel.Warning);
        public static readonly MessageDescriptor<None> PressCtrlRToRestart = Create("Press Ctrl+R to restart.", Emoji.LightBulb, LogLevel.Information);
        public static readonly MessageDescriptor<(string, string)> ApplicationKind_BlazorHosted = Create<(string, string)>("Application kind: BlazorHosted. '{0}' references BlazorWebAssembly project '{1}'.", Emoji.Default, LogLevel.Debug);
        public static readonly MessageDescriptor<None> ApplicationKind_BlazorWebAssembly = Create("Application kind: BlazorWebAssembly.", Emoji.Default, LogLevel.Debug);
        public static readonly MessageDescriptor<None> ApplicationKind_WebApplication = Create("Application kind: WebApplication.", Emoji.Default, LogLevel.Debug);
        public static readonly MessageDescriptor<None> ApplicationKind_Default = Create("Application kind: Default.", Emoji.Default, LogLevel.Debug);
        public static readonly MessageDescriptor<None> ApplicationKind_WebSockets = Create("Application kind: WebSockets.", Emoji.Default, LogLevel.Debug);
        public static readonly MessageDescriptor<int> WatchingFilesForChanges = Create<int>("Watching {0} file(s) for changes", Emoji.Watch, LogLevel.Debug);
        public static readonly MessageDescriptor<string> WatchingFilesForChanges_FilePath = Create<string>("> {0}", Emoji.Watch, LogLevel.Trace);
        public static readonly MessageDescriptor<None> LoadingProjects = Create("Loading projects ...", Emoji.Watch, LogLevel.Information);
        public static readonly MessageDescriptor<(int, double)> LoadedProjects = Create<(int, double)>("Loaded {0} project(s) in {1:0.0}s.", Emoji.Watch, LogLevel.Information);
        public static readonly MessageDescriptor<string> Building = Create<string>("Building {0} ...", Emoji.Default, LogLevel.Debug);
        public static readonly MessageDescriptor<string> BuildFailed = Create<string>("Build failed: {0}", Emoji.Default, LogLevel.Debug);
        public static readonly MessageDescriptor<string> BuildSucceeded = Create<string>("Build succeeded: {0}", Emoji.Default, LogLevel.Debug);
        public static readonly MessageDescriptor<IEnumerable<ProjectRepresentation>> BuildStartedNotification = CreateNotification<IEnumerable<ProjectRepresentation>>();
        public static readonly MessageDescriptor<(IEnumerable<ProjectRepresentation> projects, bool success)> BuildCompletedNotification = CreateNotification<(IEnumerable<ProjectRepresentation> projects, bool success)>();
    }

    internal sealed class MessageDescriptor<TArgs>(string? format, Emoji emoji, LogLevel level, EventId id)
        : MessageDescriptor(VerifyFormat(format, level), emoji, level, id)
    {
        private static string? VerifyFormat(string? format, LogLevel level)
        {
            Debug.Assert(format is null == level is LogLevel.None);
#if DEBUG
            if (format != null)
            {
                var actualArity = format.Count(c => c == '{');
                var expectedArity = typeof(TArgs) == typeof(None) ? 0
                    : typeof(TArgs).IsAssignableTo(typeof(ITuple)) ? typeof(TArgs).GenericTypeArguments.Length
                    : 1;

                Debug.Assert(actualArity == expectedArity, $"Arguments of format string '{format}' do not match the specified type: {typeof(TArgs)} (actual arity: {actualArity}, expected arity: {expectedArity})");
            }
#endif
            return format;
        }

        public string GetMessage(TArgs args)
        {
            Debug.Assert(Format != null);
            return Id.Id == 0 ? Format : string.Format(Format, LogEvents.GetArgumentValues(args));
        }
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
