// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Cli.Configuration;
using Aspire.Shared.Telemetry;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Cli.Tests.TestServices;

internal static class TestConfigSourceServices
{
    public static ConfigSourceService Create(CliExecutionContext context, IConfigSourceGitClient? git = null,
        TimeProvider? clock = null, IEnvironment? environment = null, TestConfigSourceDetector? detector = null,
        ConfigSourceConfigurationProvider? provider = null, TestInteractionService? interaction = null, bool interactive = false)
    {
        return new ConfigSourceService(context, environment ?? new TestEnvironment(), detector ?? new TestConfigSourceDetector(),
            git ?? new TestConfigSourceGitClient(), clock ?? TimeProvider.System, interaction ?? new TestInteractionService(),
            interactive ? TestHelpers.CreateInteractiveHostEnvironment() : TestHelpers.CreateNonInteractiveHostEnvironment(),
            provider ?? new ConfigSourceConfigurationProvider(), NullLogger<ConfigSourceService>.Instance);
    }
}

internal sealed class TestConfigSourceDetector : IInternalMicrosoftDetector
{
    public int Calls { get; private set; }
    public bool IsInternal { get; set; }
    public string? Outcome { get; set; }

    public Task<InternalMicrosoftDetectionResult> IsInternalMicrosoftMachineAsync(CancellationToken cancellationToken = default)
    {
        Calls++;
        return Task.FromResult(new InternalMicrosoftDetectionResult(IsInternal, null, null, null,
            Outcome ?? (IsInternal ? InternalMicrosoftDetectorOutcome.Detected : InternalMicrosoftDetectorOutcome.NotDetected),
            InternalMicrosoftDetectorCacheStatus.Miss, TimeSpan.Zero, []));
    }
}

internal sealed class TestConfigSourceGitClient : IConfigSourceGitClient
{
    private int _calls;
    public int Calls => _calls;
    public Func<ConfigSource, CancellationToken, Task<(string Commit, JsonObject Configuration)>> Acquire { get; set; }
        = (_, _) => Task.FromResult(("commit-1", new JsonObject()));

    public Task<(string Commit, JsonObject Configuration)> AcquireAsync(ConfigSource source, string directory, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _calls);
        return Acquire(source, cancellationToken);
    }
}
