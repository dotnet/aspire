// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Watch
{
    internal sealed class ProcessSpec
    {
        public string? Executable { get; set; }
        public string? WorkingDirectory { get; set; }
        public Dictionary<string, string> EnvironmentVariables { get; } = [];
        public IReadOnlyList<string>? Arguments { get; set; }
        public string? EscapedArguments { get; set; }
        public Action<OutputLine>? OnOutput { get; set; }
        public ProcessExitAction? OnExit { get; set; }
        public CancellationToken CancelOutputCapture { get; set; }
        public bool UseShellExecute { get; set; }

        /// <summary>
        /// True if the process is a user application, false if it is a helper process (e.g. dotnet build).
        /// </summary>
        public bool IsUserApplication { get; set; }

        public string? ShortDisplayName()
            => Path.GetFileNameWithoutExtension(Executable);

        public string GetArgumentsDisplay()
            => EscapedArguments ?? CommandLineUtilities.JoinArguments(Arguments ?? []);
    }
}
