// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestUtilities;
using Xunit;

namespace Infrastructure.Tests.Pipelines;

/// <summary>
/// Runs the extension E2E recorder helper inside the same wrapper GitHub Actions builds around a
/// <c>pwsh</c> step, pinning the contract that the helper can never fail a shard.
///
/// ffmpeg only powers the diagnostic screen recordings, and run-e2e.js already skips recording with
/// a warning when the binary is missing, so a failed install has to stay a warning. That was not
/// true once: bounding the apt-get calls with <c>timeout</c> stopped them hanging, but the failed
/// exit code then leaked out and failed six shards outright, because GitHub appends
/// <c>if ((Test-Path -LiteralPath variable:/LASTEXITCODE)) { exit $LASTEXITCODE }</c> to every pwsh
/// step. See
/// https://docs.github.com/actions/reference/workflows-and-actions/workflow-syntax#exit-codes-and-error-action-preference.
///
/// Running the helper inside the appended epilogue catches that regression: without the explicit
/// <c>exit 0</c>, the leftover <c>$LASTEXITCODE</c> turns an optional recorder failure into a shard
/// failure.
/// </summary>
public sealed class ExtensionE2eRecorderStepTests(ITestOutputHelper testOutput)
{
    private static readonly string s_recorderScriptPath = Path.Combine(
        RepoRoot.Path,
        ".github",
        "scripts",
        "install-extension-e2e-recorder.ps1");

    /// <summary>Exit code the apt-get stub uses for a failed install, distinct from the 124 that a real timeout returns.</summary>
    private const int StubAptFailureExitCode = 100;

    /// <summary>What GitHub wraps around the <c>run:</c> body of a pwsh step.</summary>
    private const string GitHubPwshPrologue = "$ErrorActionPreference = 'stop'";
    private const string GitHubPwshEpilogue = "if ((Test-Path -LiteralPath variable:/LASTEXITCODE)) { exit $LASTEXITCODE }";

    private const string RetryWarning = "::warning::Attempt 1 to install ffmpeg failed; retrying.";
    private const string GaveUpWarning = "::warning::ffmpeg could not be installed; E2E screen recordings are disabled for this shard.";

    [Theory]
    // As authored: every apt-get outcome has to leave the shard alive.
    [InlineData("always-fails", 2, new[] { RetryWarning, GaveUpWarning })]
    [InlineData("fails-once", 2, new[] { RetryWarning })]
    [InlineData("succeeds", 1, new string[0])]
    [RequiresTools(["pwsh"])]
    public async Task RecorderStepSurvivesEveryAptOutcome(
        string aptBehavior,
        int expectedInstallAttempts,
        string[] expectedWarnings)
    {
        // The stubs below are shell scripts, and this step only ever runs on the Linux shards
        // (`if: ${{ matrix.useXvfb }}`), so there is nothing to pin on Windows.
        Assert.SkipWhen(OperatingSystem.IsWindows(), "The recorder step and its stubs are Unix-only.");

        using var workspace = TemporaryWorkspace.Create(testOutput);

        var stubDirectory = CreateCommandStubs(workspace, aptBehavior, out var invocationLog, out var pwshPath);
        var scriptPath = Path.Combine(workspace.Path, "step.ps1");
        var escapedRecorderScriptPath = s_recorderScriptPath.Replace("'", "''", StringComparison.Ordinal);
        File.WriteAllText(
            scriptPath,
            $$"""
            {{GitHubPwshPrologue}}
            function Start-Sleep { param([int]$Seconds) }
            & '{{escapedRecorderScriptPath}}'
            {{GitHubPwshEpilogue}}
            """);

        using var command = new PowerShellCommand(scriptPath, testOutput, label: aptBehavior)
            .WithEnvironmentVariable("PATH", stubDirectory)
            .WithTimeout(TimeSpan.FromMinutes(2));
        var result = await command.ExecuteAsync();

        var invocations = File.Exists(invocationLog) ? File.ReadAllLines(invocationLog) : [];
        var installAttempts = invocations.Count(line => line.StartsWith("apt-get install", StringComparison.Ordinal));

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(expectedInstallAttempts, installAttempts);

        // Distinguishes a genuine retry from a run that merely failed twice: both make two install
        // attempts, and only the warnings say whether the second one succeeded. Without this, a
        // stub that could not track attempts would quietly degrade fails-once into always-fails.
        var warnings = result.Output
            .Split('\n')
            .Select(line => line.Trim('\r'))
            .Where(line => line.StartsWith("::warning::", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(expectedWarnings, warnings);

        // Plain `timeout` only sends SIGTERM, and apt/dpkg defer signals around critical sections,
        // so without the escalation a wedged child can outlive its own timeout - the hang this step
        // was bounded to prevent.
        var timeoutInvocations = invocations
            .Where(line => line.StartsWith("timeout ", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(expectedInstallAttempts * 2, timeoutInvocations.Length);
        for (var attempt = 0; attempt < expectedInstallAttempts; attempt++)
        {
            Assert.EndsWith("apt-get update", timeoutInvocations[attempt * 2], StringComparison.Ordinal);
            Assert.Contains("apt-get install", timeoutInvocations[(attempt * 2) + 1], StringComparison.Ordinal);
        }

        Assert.All(
            timeoutInvocations,
            line => Assert.Contains("--kill-after=", line, StringComparison.Ordinal));

        Assert.NotEmpty(pwshPath);
    }

    /// <summary>
    /// Builds a directory of stub executables that becomes the whole PATH for the step.
    ///
    /// Replacing PATH rather than prepending is deliberate: the step's first branch is
    /// <c>Get-Command ffmpeg</c>, so a machine that happens to have ffmpeg installed would otherwise
    /// silently skip the install path these tests exist to cover. pwsh is linked in because it has
    /// to stay resolvable once PATH is replaced.
    /// </summary>
    private static string CreateCommandStubs(TemporaryWorkspace workspace, string aptBehavior, out string invocationLog, out string pwshPath)
    {
        var stubDirectory = workspace.CreateDirectory("stubs").FullName;
        invocationLog = Path.Combine(workspace.Path, "invocations.log");
        var attemptCounter = Path.Combine(workspace.Path, "install-attempts");

        // `sudo timeout --kill-after=30 180 apt-get ...`, so both wrappers have to hand off to the
        // rest of the command line for the apt-get stub to ever run.
        WriteStub(stubDirectory, "sudo", $"""
            #!/bin/sh
            echo "sudo $*" >> "{invocationLog}"
            exec "$@"
            """);

        WriteStub(stubDirectory, "timeout", $"""
            #!/bin/sh
            echo "timeout $*" >> "{invocationLog}"
            while [ $# -gt 0 ]; do
              case "$1" in
                --kill-after=*) shift ;;
                -k) shift 2 ;;
                *) break ;;
              esac
            done
            shift
            exec "$@"
            """);

        // Only the install decides success; `apt-get update` is allowed to fail because the install
        // can still succeed from the indices already on the runner image.
        //
        // The attempt count is kept with shell builtins alone. PATH is replaced by this directory,
        // so external tools are deliberately unreachable, and reaching for one here would silently
        // read zero attempts every time and turn the fails-once case into a second always-fails run.
        WriteStub(stubDirectory, "apt-get", $"""
            #!/bin/sh
            echo "apt-get $*" >> "{invocationLog}"
            if [ "$1" != "install" ]; then exit 0; fi
            echo attempt >> "{attemptCounter}"
            attempts=0
            while read -r _line; do attempts=$((attempts + 1)); done < "{attemptCounter}"
            case "{aptBehavior}" in
              succeeds) exit 0 ;;
              fails-once) [ "$attempts" -ge 2 ] && exit 0; exit {StubAptFailureExitCode} ;;
              *) exit {StubAptFailureExitCode} ;;
            esac
            """);

        pwshPath = PathLookupHelper.FindFullPathFromPath("pwsh") ?? string.Empty;
        Assert.SkipWhen(pwshPath.Length == 0, "pwsh is not on PATH.");
        File.CreateSymbolicLink(Path.Combine(stubDirectory, "pwsh"), pwshPath);

        return stubDirectory;
    }

    private static void WriteStub(string directory, string name, string contents)
    {
        var stubPath = Path.Combine(directory, name);
        File.WriteAllText(stubPath, contents);

        // The test skips on Windows before reaching this, but Assert.SkipWhen is invisible to the
        // platform-compatibility analyzer, so the guard has to be one it can see.
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(stubPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }
}
