// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch
{
    internal sealed class DotNetWatchContext : IDisposable
    {
        public const string DefaultLogComponentName = $"{nameof(DotNetWatchContext)}:Default";
        public const string BuildLogComponentName = $"{nameof(DotNetWatchContext)}:Build";

        public required GlobalOptions Options { get; init; }
        public required EnvironmentOptions EnvironmentOptions { get; init; }
        public required IProcessOutputReporter ProcessOutputReporter { get; init; }
        public required ILogger Logger { get; init; }
        public required ILogger BuildLogger { get; init; }
        public required ILoggerFactory LoggerFactory { get; init; }
        public required ProcessRunner ProcessRunner { get; init; }

        /// <summary>
        /// Roots of the project graph to watch.
        /// </summary>
        public required ImmutableArray<ProjectRepresentation> RootProjects { get; init; }

        /// <summary>
        /// Options for launching a main project. If null no main project is being launched.
        /// </summary>
        public required ProjectOptions? MainProjectOptions { get; init; }

        /// <summary>
        /// Default target framework.
        /// </summary>
        public required string? TargetFramework { get; init; }

        /// <summary>
        /// Additional arguments passed to `dotnet build` when building projects.
        /// </summary>
        public required IReadOnlyList<string> BuildArguments { get; init; }

        public required BrowserRefreshServerFactory BrowserRefreshServerFactory { get; init; }
        public required BrowserLauncher BrowserLauncher { get; init; }

        public void Dispose()
        {
            BrowserRefreshServerFactory.Dispose();
        }
    }
}
