// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp.Process;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.SecretManager.Tools.Internal;

namespace Aspire.Hosting;

internal sealed class VersionChecker : BackgroundService
{
    private static readonly TimeSpan s_checkInterval = TimeSpan.FromDays(1);

    private const string CheckDateKey = "Aspire.Hosting.VersionChecker.LastCheckDate";
    private const string KnownLastestVersionDateKey = "Aspire.Hosting.VersionChecker.KnownLastestVersion";
    private const string IgnoreVersionKey = "Aspire.Hosting.VersionChecker.IgnoreVersion";

    private readonly IInteractionService _interactionService;
    private readonly ILogger<VersionChecker> _logger;
    private readonly IConfiguration _configuration;
    private readonly DistributedApplicationOptions _options;
    private readonly Version? _appHostVersion;

    public VersionChecker(IInteractionService interactionService, ILogger<VersionChecker> logger, IConfiguration configuration, DistributedApplicationOptions options)
    {
        _interactionService = interactionService;
        _logger = logger;
        _configuration = configuration;
        _options = options;

        _appHostVersion = typeof(VersionChecker).Assembly.GetName().Version;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var checkForLatestVersion = true;
        if (_configuration[CheckDateKey] is string checkDateString &&
            DateTime.TryParseExact(checkDateString, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var checkDate))
        {
            if (DateTime.UtcNow - checkDate < s_checkInterval)
            {
                // Already checked within the last day.
                checkForLatestVersion = false;
            }
        }

        Version? latestVersion = null;
        if (checkForLatestVersion)
        {
            latestVersion = await TryCheckForLatestVersionAsync(stoppingToken).ConfigureAwait(false);
        }

        if (TryGetConfigVersion(KnownLastestVersionDateKey, out var storedKnownLatestVersion))
        {
            if (latestVersion == null)
            {
                // Use the known latest version if we can't check for the latest version.
                latestVersion = storedKnownLatestVersion;
            }
        }

        if (latestVersion == null || latestVersion <= _appHostVersion)
        {
            return;
        }

        if (TryGetConfigVersion(IgnoreVersionKey, out var ignoreVersion))
        {
            if (latestVersion <= ignoreVersion)
            {
                // Ignore this version.
                _logger.LogInformation("Ignoring version {Version} as it is less than or equal to the ignored version {IgnoreVersion}.", latestVersion, ignoreVersion);
                return;
            }
        }

        if (latestVersion > storedKnownLatestVersion)
        {
            SecretsStore.TrySetUserSecret(_options.Assembly, KnownLastestVersionDateKey, latestVersion.ToString());
        }

        var result = await _interactionService.PromptMessageBarAsync(
            title: "Update now",
            message: $"Aspire {latestVersion} is available.",
            options: new MessageBarInteractionOptions
            {
                LinkText = "Upgrade instructions",
                PrimaryButtonText = "Ignore"
            },
            cancellationToken: stoppingToken).ConfigureAwait(false);

        if (result.Data)
        {
            SecretsStore.TrySetUserSecret(_options.Assembly, KnownLastestVersionDateKey, latestVersion.ToString());
        }
    }

    private bool TryGetConfigVersion(string key, [NotNullWhen(true)] out Version? knownLatestVersion)
    {
        if (_configuration[key] is string latestVersionString &&
            Version.TryParse(latestVersionString, out knownLatestVersion))
        {
            return true;
        }

        knownLatestVersion = null;
        return false;
    }

    private async Task<Version?> TryCheckForLatestVersionAsync(CancellationToken stoppingToken)
    {
        var outputJson = new StringBuilder();
        var spec = new ProcessSpec("dotnet")
        {
            Arguments = "package search Aspire.Hosting.AppHost --format json",
            ThrowOnNonZeroReturnCode = false,
            InheritEnv = true,
            OnOutputData = output =>
            {
                outputJson.Append(output);
                _logger.LogDebug("dotnet (stdout): {Output}", output);
            },
            OnErrorData = error =>
            {
                _logger.LogDebug("dotnet (stderr): {Error}", error);
            },
        };

        _logger.LogInformation("Running dotnet CLI to check for latest version with arguments: {ArgumentList}", spec.Arguments);
        var (pendingProcessResult, processDisposable) = ProcessUtil.Run(spec);

        await using (processDisposable)
        {
            var processResult = await pendingProcessResult
                .WaitAsync(stoppingToken)
                .ConfigureAwait(false);

            if (processResult.ExitCode != 0)
            {
                _logger.LogError("The dotnet CLI call to check for latest version failed with exit code {ExitCode}.", processResult.ExitCode);
                return null;
            }
        }

        var packages = ParseOutput(outputJson.ToString());
        var versions = new List<Version>();
        foreach (var package in packages)
        {
            if (package.LatestVersion.Contains('-'))
            {
                // Pre-release. Ignore.
                continue;
            }

            if (Version.TryParse(package.LatestVersion, out var version))
            {
                versions.Add(version);
            }
        }

        return versions.Order().FirstOrDefault();
    }

    private static List<Package> ParseOutput(string outputJson)
    {
        var packages = new List<Package>();

        using var document = JsonDocument.Parse(outputJson);
        var root = document.RootElement;

        if (root.TryGetProperty("searchResult", out var searchResults))
        {
            foreach (var result in searchResults.EnumerateArray())
            {
                if (result.TryGetProperty("packages", out var packagesArray))
                {
                    foreach (var pkg in packagesArray.EnumerateArray())
                    {
                        var id = pkg.GetProperty("id").GetString();
                        var latestVersion = pkg.GetProperty("latestVersion").GetString();
                        if (id != null && latestVersion != null)
                        {
                            packages.Add(new Package(id, latestVersion));
                        }
                    }
                }
            }
        }

        return packages;
    }

    private record Package(string Id, string LatestVersion);
}

#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
