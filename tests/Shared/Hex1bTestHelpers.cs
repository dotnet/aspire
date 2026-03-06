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
    /// Starter App (ASP.NET Core/Blazor) — the default first option.
    /// Prompts: template, project name, output path, URLs, Redis, test project.
    /// </summary>
    Starter,

    /// <summary>
    /// Starter App (ASP.NET Core/React) — 2nd option.
    /// Prompts: template, project name, output path, URLs, Redis. No test project prompt.
    /// </summary>
    JsReact,

    /// <summary>
    /// Starter App (FastAPI/React) — 3rd option.
    /// Prompts: template, project name, output path, URLs, Redis. No test project prompt.
    /// </summary>
    PythonReact,

    /// <summary>
    /// Starter App (Express/React) — 4th option.
    /// Prompts: template, project name, output path, URLs. No Redis or test project prompt.
    /// </summary>
    ExpressReact,

    /// <summary>
    /// Empty AppHost — 5th option.
    /// Prompts: template, language (C#), project name, output path, URLs. No Redis or test project prompt.
    /// </summary>
    EmptyAppHost,
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
    /// Waits for a successful command prompt, but fails fast if an error prompt is detected.
    /// Unlike <see cref="WaitForSuccessPrompt"/>, this method also watches for error prompts
    /// (ERR:N pattern) and throws immediately instead of waiting for the full timeout.
    /// Use this for commands that may fail due to transient errors (e.g., CLI downloads).
    /// </summary>
    internal static Hex1bTerminalInputSequenceBuilder WaitForSuccessPromptFailFast(
        this Hex1bTerminalInputSequenceBuilder builder,
        SequenceCounter counter,
        TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(500);
        var sawError = false;

        return builder.WaitUntil(snapshot =>
            {
                var successSearcher = new CellPatternSearcher()
                    .FindPattern(counter.Value.ToString())
                    .RightText(" OK] $ ");

                if (successSearcher.Search(snapshot).Count > 0)
                {
                    return true;
                }

                var errorSearcher = new CellPatternSearcher()
                    .FindPattern(counter.Value.ToString())
                    .RightText(" ERR:");

                if (errorSearcher.Search(snapshot).Count > 0)
                {
                    sawError = true;
                    return true;
                }

                return false;
            }, effectiveTimeout)
            .WaitUntil(_ =>
            {
                if (sawError)
                {
                    throw new InvalidOperationException(
                        $"Command failed with non-zero exit code (detected ERR prompt at sequence {counter.Value}). Check the terminal recording for details.");
                }

                counter.Increment();
                return true;
            }, TimeSpan.FromSeconds(1));
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

    /// <summary>
    /// Handles the agent init confirmation prompt that appears after <c>aspire init</c> or <c>aspire new</c>.
    /// Declines the prompt so the command exits cleanly.
    /// </summary>
    internal static Hex1bTerminalInputSequenceBuilder DeclineAgentInitPrompt(
        this Hex1bTerminalInputSequenceBuilder builder)
    {
        var agentInitPrompt = new CellPatternSearcher()
            .Find("configure AI agent environments");

        return builder
            .WaitUntil(s => agentInitPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .Wait(500)
            .Type("n")
            .Enter();
    }

    /// <summary>
    /// Runs <c>aspire new</c> interactively, selecting the specified template and responding to all prompts.
    /// This centralizes the multi-step interactive flow so that changes to <c>aspire new</c> prompts
    /// only need to be updated in one place instead of across every E2E test.
    /// </summary>
    /// <param name="builder">The sequence builder.</param>
    /// <param name="projectName">The project name to enter at the prompt.</param>
    /// <param name="counter">The sequence counter for prompt detection.</param>
    /// <param name="template">The template to select. Defaults to <see cref="AspireTemplate.Starter"/>.</param>
    /// <param name="useRedisCache">
    /// Whether to enable Redis Cache. Defaults to <c>true</c> (the <c>aspire new</c> default).
    /// Only applies to templates that show the Redis prompt (Starter, JsReact, PythonReact).
    /// </param>
    /// <param name="createTestProject">
    /// Whether to create a test project. Defaults to <c>false</c> (the <c>aspire new</c> default).
    /// Only applies to the Starter template.
    /// </param>
    /// <returns>The builder for chaining.</returns>
    internal static Hex1bTerminalInputSequenceBuilder AspireNew(
        this Hex1bTerminalInputSequenceBuilder builder,
        string projectName,
        SequenceCounter counter,
        AspireTemplate template = AspireTemplate.Starter,
        bool useRedisCache = true,
        bool createTestProject = false)
    {
        var templateTimeout = TimeSpan.FromSeconds(60);

        // Wait for the template selection list to appear.
        // The first item "> Starter App" is always highlighted initially.
        var waitingForTemplateList = new CellPatternSearcher()
            .Find("> Starter App");

        var waitingForProjectNamePrompt = new CellPatternSearcher()
            .Find("Enter the project name");

        var waitingForOutputPathPrompt = new CellPatternSearcher()
            .Find("Enter the output path:");

        var waitingForUrlsPrompt = new CellPatternSearcher()
            .Find("Use *.dev.localhost URLs");

        // Step 1: Type aspire new and wait for the template list
        builder.Type("aspire new")
            .Enter()
            .WaitUntil(s => waitingForTemplateList.Search(s).Count > 0, templateTimeout);

        // Step 2: Navigate to and select the desired template
        switch (template)
        {
            case AspireTemplate.Starter:
                builder.Enter(); // First option, no navigation needed
                break;

            case AspireTemplate.JsReact:
                var jsReactSelected = new CellPatternSearcher()
                    .Find("> Starter App (ASP.NET Core/React)");
                builder.Key(Hex1bKey.DownArrow)
                    .WaitUntil(s => jsReactSelected.Search(s).Count > 0, TimeSpan.FromSeconds(5))
                    .Enter();
                break;

            case AspireTemplate.PythonReact:
                var pythonReactSelected = new CellPatternSearcher()
                    .Find("> Starter App (FastAPI/React)");
                builder.Key(Hex1bKey.DownArrow)
                    .Key(Hex1bKey.DownArrow)
                    .WaitUntil(s => pythonReactSelected.Search(s).Count > 0, TimeSpan.FromSeconds(5))
                    .Enter();
                break;

            case AspireTemplate.ExpressReact:
                var expressReactSelected = new CellPatternSearcher()
                    .Find("> Starter App (Express/React)");
                builder.Key(Hex1bKey.DownArrow)
                    .Key(Hex1bKey.DownArrow)
                    .Key(Hex1bKey.DownArrow)
                    .WaitUntil(s => expressReactSelected.Search(s).Count > 0, TimeSpan.FromSeconds(5))
                    .Enter();
                break;

            case AspireTemplate.EmptyAppHost:
                var emptyAppHostSelected = new CellPatternSearcher()
                    .Find("> Empty AppHost");
                builder.Key(Hex1bKey.DownArrow)
                    .Key(Hex1bKey.DownArrow)
                    .Key(Hex1bKey.DownArrow)
                    .Key(Hex1bKey.DownArrow)
                    .WaitUntil(s => emptyAppHostSelected.Search(s).Count > 0, TimeSpan.FromSeconds(5))
                    .Enter()
                    .Enter(); // Select C# language
                break;
        }

        // Step 3: Enter project name
        builder.WaitUntil(s => waitingForProjectNamePrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Type(projectName)
            .Enter();

        // Step 4: Accept default output path
        builder.WaitUntil(s => waitingForOutputPathPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter();

        // Step 5: URLs prompt (all templates have this)
        builder.WaitUntil(s => waitingForUrlsPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter(); // Accept default "No"

        // Step 6: Redis prompt (only Starter, JsReact, PythonReact)
        if (template is AspireTemplate.Starter or AspireTemplate.JsReact or AspireTemplate.PythonReact)
        {
            var waitingForRedisPrompt = new CellPatternSearcher()
                .Find("Use Redis Cache");
            builder.WaitUntil(s => waitingForRedisPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10));

            if (!useRedisCache)
            {
                // Default is "Yes", navigate to "No"
                builder.Key(Hex1bKey.DownArrow);
            }

            builder.Enter();
        }

        // Step 7: Test project prompt (only Starter)
        if (template is AspireTemplate.Starter)
        {
            var waitingForTestPrompt = new CellPatternSearcher()
                .Find("Do you want to create a test project?");
            builder.WaitUntil(s => waitingForTestPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10));

            if (createTestProject)
            {
                // Default is "No", navigate to "Yes"
                builder.Key(Hex1bKey.UpArrow);
            }

            builder.Enter();
        }

        // Step 8: Decline the agent init prompt
        builder.DeclineAgentInitPrompt();

        // Wait for project creation to complete
        return builder.WaitForSuccessPrompt(counter);
    }
}