// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Hex1b;
using Hex1b.Automation;

namespace Aspire.Tests.Shared;

/// <summary>
/// Tracks the sequence number for shell prompt detection in Hex1b terminal sessions.
/// </summary>
internal sealed class SequenceCounter
{
    public int Value { get; private set; } = 1;

    public int Increment()
    {
        return ++Value;
    }
}

/// <summary>
/// Shared helper methods for creating and managing Hex1b terminal sessions across E2E test projects.
/// </summary>
internal static class Hex1bTestHelpers
{
    /// <summary>
    /// Creates a headless Hex1b terminal configured for E2E testing with asciinema recording.
    /// Uses default dimensions of 160x48 unless overridden.
    /// </summary>
    /// <param name="testName">The test name used for the recording file path. Defaults to the calling method name.</param>
    /// <param name="localSubDir">The subdirectory name under the temp folder for local (non-CI) recordings.</param>
    /// <param name="width">The terminal width in columns. Defaults to 160.</param>
    /// <param name="height">The terminal height in rows. Defaults to 48.</param>
    /// <returns>A configured <see cref="Hex1bTerminal"/> instance. Caller is responsible for disposal.</returns>
    internal static Hex1bTerminal CreateTestTerminal(
        string localSubDir,
        int width = 160,
        int height = 48,
        [CallerMemberName] string testName = "")
    {
        var recordingPath = GetTestResultsRecordingPath(testName, localSubDir);

        var builder = Hex1bTerminal.CreateBuilder()
            .WithHeadless()
            .WithDimensions(width, height)
            .WithAsciinemaRecording(recordingPath)
            .WithPtyProcess("/bin/bash", ["--norc"]);

        return builder.Build();
    }

    /// <summary>
    /// Gets the path for storing asciinema recordings that will be uploaded as CI artifacts.
    /// In CI, this returns a path under $GITHUB_WORKSPACE/testresults/recordings/.
    /// Locally, this returns a path under the system temp directory.
    /// </summary>
    /// <param name="testName">The name of the test (used as the recording filename).</param>
    /// <param name="localSubDir">The subdirectory name under the temp folder for local (non-CI) recordings.</param>
    /// <returns>The full path to the .cast recording file.</returns>
    internal static string GetTestResultsRecordingPath(string testName, string localSubDir)
    {
        var githubWorkspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
        string recordingsDir;

        if (!string.IsNullOrEmpty(githubWorkspace))
        {
            // CI environment - write directly to test results for artifact upload
            recordingsDir = Path.Combine(githubWorkspace, "testresults", "recordings");
        }
        else
        {
            // Local development - use temp directory
            recordingsDir = Path.Combine(Path.GetTempPath(), localSubDir, "recordings");
        }

        Directory.CreateDirectory(recordingsDir);
        return Path.Combine(recordingsDir, $"{testName}.cast");
    }

    /// <summary>
    /// Waits for a successful command prompt with the expected sequence number.
    /// </summary>
    internal static Hex1bTerminalInputSequenceBuilder WaitForSuccessPrompt(
        this Hex1bTerminalInputSequenceBuilder builder,
        SequenceCounter counter,
        TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(500);

        return builder.WaitUntil(snapshot =>
            {
                var successPromptSearcher = new CellPatternSearcher()
                    .FindPattern(counter.Value.ToString())
                    .RightText(" OK] $ ");

                var result = successPromptSearcher.Search(snapshot);
                return result.Count > 0;
            }, effectiveTimeout)
            .IncrementSequence(counter);
    }

    /// <summary>
    /// Waits for any prompt (success or error) matching the current sequence counter.
    /// Use this when the command is expected to return a non-zero exit code.
    /// </summary>
    internal static Hex1bTerminalInputSequenceBuilder WaitForAnyPrompt(
        this Hex1bTerminalInputSequenceBuilder builder,
        SequenceCounter counter,
        TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(500);

        return builder.WaitUntil(snapshot =>
            {
                var successSearcher = new CellPatternSearcher()
                    .FindPattern(counter.Value.ToString())
                    .RightText(" OK] $ ");
                var errorSearcher = new CellPatternSearcher()
                    .FindPattern(counter.Value.ToString())
                    .RightText(" ERR:");

                return successSearcher.Search(snapshot).Count > 0 || errorSearcher.Search(snapshot).Count > 0;
            }, effectiveTimeout)
            .IncrementSequence(counter);
    }

    /// <summary>
    /// Waits for the shell prompt to show a non-zero exit code pattern: [N ERR:code] $
    /// This is used to verify that a command exited with a failure code.
    /// </summary>
    /// <param name="builder">The sequence builder.</param>
    /// <param name="counter">The sequence counter for prompt detection.</param>
    /// <param name="exitCode">The expected non-zero exit code.</param>
    /// <param name="timeout">Optional timeout (defaults to 500 seconds).</param>
    /// <returns>The builder for chaining.</returns>
    internal static Hex1bTerminalInputSequenceBuilder WaitForErrorPrompt(
        this Hex1bTerminalInputSequenceBuilder builder,
        SequenceCounter counter,
        int exitCode = 1,
        TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(500);

        return builder.WaitUntil(snapshot =>
            {
                var errorPromptSearcher = new CellPatternSearcher()
                    .FindPattern(counter.Value.ToString())
                    .RightText($" ERR:{exitCode}] $ ");

                var result = errorPromptSearcher.Search(snapshot);
                return result.Count > 0;
            }, effectiveTimeout)
            .IncrementSequence(counter);
    }

    /// <summary>
    /// Increments the sequence counter.
    /// </summary>
    internal static Hex1bTerminalInputSequenceBuilder IncrementSequence(
        this Hex1bTerminalInputSequenceBuilder builder,
        SequenceCounter counter)
    {
        return builder.WaitUntil(s =>
        {
            counter.Increment();
            return true;
        }, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Executes an arbitrary callback action during the sequence execution.
    /// This is useful for performing file modifications or other side effects between terminal commands.
    /// </summary>
    /// <param name="builder">The sequence builder.</param>
    /// <param name="callback">The callback action to execute.</param>
    /// <returns>The builder for chaining.</returns>
    internal static Hex1bTerminalInputSequenceBuilder ExecuteCallback(
        this Hex1bTerminalInputSequenceBuilder builder,
        Action callback)
    {
        return builder.WaitUntil(s =>
        {
            callback();
            return true;
        }, TimeSpan.FromSeconds(1));
    }
}
