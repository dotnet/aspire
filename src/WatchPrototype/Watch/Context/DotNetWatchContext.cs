// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        public required ProjectOptions RootProjectOptions { get; init; }

        public required BrowserRefreshServerFactory BrowserRefreshServerFactory { get; init; }
        public required BrowserLauncher BrowserLauncher { get; init; }

        public void Dispose()
        {
            BrowserRefreshServerFactory.Dispose();
        }
    }
}
