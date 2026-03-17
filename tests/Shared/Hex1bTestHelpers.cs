// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Hex1b;
using Hex1b.Automation;
using Hex1b.Input;

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
/// Represents the available Aspire project templates for <c>aspire new</c>.
/// The enum values correspond to the order in the template selection list.
/// </summary>
internal enum AspireTemplate
{
    /// <summary>
    /// Starter App (ASP.NET Core/Blazor) — the 1st option (default).
    /// Prompts: template, project name, output path, URLs, Redis, test project.
    /// </summary>
    Starter,

    /// <summary>
    /// Starter App (ASP.NET Core/React) — 2nd option.
    /// Prompts: template, project name, output path, URLs, Redis. No test project prompt.
    /// </summary>
    JsReact,

    /// <summary>
    /// Starter App (Express/React) — 3rd option.
    /// Prompts: template, project name, output path, URLs. No Redis or test project prompt.
    /// </summary>
    ExpressReact,

    /// <summary>
    /// Starter App (FastAPI/React) — 4th option.
    /// Prompts: template, project name, output path, URLs, Redis. No test project prompt.
    /// </summary>
    PythonReact,

    /// <summary>
    /// Empty (C# AppHost) — 5th option.
    /// Prompts: template, project name, output path, URLs. No language, Redis, or test project prompt.
    /// </summary>
    EmptyAppHost,

    /// <summary>
    /// Empty (TypeScript AppHost) — 6th option.
    /// Prompts: template, project name, output path, URLs. No language, Redis, or test project prompt.
    /// </summary>
    TypeScriptEmptyAppHost,
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
}
