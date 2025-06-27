// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.SecretManager.Tools.Internal;
using Semver;

namespace Aspire.Hosting.VersionChecking;

internal sealed class VersionCheckService : BackgroundService
{
    private static readonly TimeSpan s_checkInterval = TimeSpan.FromDays(1);

    internal const string CheckDateKey = "Aspire.Hosting.VersionChecker.LastCheckDate";
    internal const string KnownLastestVersionDateKey = "Aspire.Hosting.VersionChecker.KnownLastestVersion";
    internal const string IgnoreVersionKey = "Aspire.Hosting.VersionChecker.IgnoreVersion";

    private readonly IInteractionService _interactionService;
    private readonly ILogger<VersionCheckService> _logger;
    private readonly IConfiguration _configuration;
    private readonly DistributedApplicationOptions _options;
    private readonly IVersionFetcher _versionFetcher;
    private readonly DistributedApplicationExecutionContext _executionContext;
    private readonly TimeProvider _timeProvider;
    private readonly SemVersion? _appHostVersion;

    public VersionCheckService(IInteractionService interactionService, ILogger<VersionCheckService> logger,
        IConfiguration configuration, DistributedApplicationOptions options, IVersionFetcher versionFetcher,
        DistributedApplicationExecutionContext executionContext, TimeProvider timeProvider)
    {
        _interactionService = interactionService;
        _logger = logger;
        _configuration = configuration;
        _options = options;
        _versionFetcher = versionFetcher;
        _executionContext = executionContext;
        _timeProvider = timeProvider;

        var version = typeof(VersionCheckService).Assembly.GetName().Version!;
        var patch = version.Build > 0 ? version.Build : 0;
        _appHostVersion = new SemVersion(version.Major, version.Minor, patch);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_interactionService.IsAvailable || _executionContext.IsPublishMode)
        {
            // Don't check version if there is no way to prompt that information to the user.
            // Or app is being run during a publish.
            return;
        }

        try
        {
            await CheckForLatestAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Ignore errors during shutdown.
            if (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogDebug(ex, "Error checking for latest version.");
            }
        }
    }

    private async Task CheckForLatestAsync(CancellationToken stoppingToken)
    {
        var now = _timeProvider.GetUtcNow();
        var checkForLatestVersion = true;
        if (_configuration[CheckDateKey] is string checkDateString &&
            DateTime.TryParseExact(checkDateString, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var checkDate))
        {
            if (now - checkDate < s_checkInterval)
            {
                // Already checked within the last day.
                checkForLatestVersion = false;
            }
        }

        SemVersion? latestVersion = null;
        if (checkForLatestVersion)
        {
            SecretsStore.TrySetUserSecret(_options.Assembly, CheckDateKey, now.ToString("o", CultureInfo.InvariantCulture));
            latestVersion = await _versionFetcher.TryFetchLatestVersionAsync(stoppingToken).ConfigureAwait(false);
        }

        if (TryGetConfigVersion(KnownLastestVersionDateKey, out var storedKnownLatestVersion))
        {
            if (latestVersion == null)
            {
                // Use the known latest version if we can't check for the latest version.
                latestVersion = storedKnownLatestVersion;
            }
        }

        if (latestVersion == null || IsVersionGreaterOrEqual(_appHostVersion, latestVersion))
        {
            // App host version is greater than the latest version so exit.
            return;
        }

        if (TryGetConfigVersion(IgnoreVersionKey, out var ignoreVersion))
        {
            if (IsVersionGreaterOrEqual(ignoreVersion, latestVersion))
            {
                // Ignored version is greater or equal to latest version so exit.
                _logger.LogDebug("Ignoring version {Version} as it is less than or equal to the ignored version {IgnoreVersion}.", latestVersion, ignoreVersion);
                return;
            }
        }

        if (IsVersionGreater(latestVersion, storedKnownLatestVersion) || storedKnownLatestVersion == null)
        {
            // Latest version is greater than the stored known latest version, so update it.
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
            // User chose to ignore. Set the latest version as the ignored version.
            SecretsStore.TrySetUserSecret(_options.Assembly, IgnoreVersionKey, latestVersion.ToString());
        }

        return;
    }

    public static bool IsVersionGreaterOrEqual(SemVersion? version1, SemVersion? version2)
    {
        if (version1 == null || version2 == null)
        {
            return false;
        }
        return SemVersion.ComparePrecedence(version1, version2) >= 0;
    }

    public static bool IsVersionGreater(SemVersion? version1, SemVersion? version2)
    {
        if (version1 == null || version2 == null)
        {
            return false;
        }
        return SemVersion.ComparePrecedence(version1, version2) > 0;
    }

    private bool TryGetConfigVersion(string key, [NotNullWhen(true)] out SemVersion? knownLatestVersion)
    {
        if (_configuration[key] is string latestVersionString &&
            SemVersion.TryParse(latestVersionString, out knownLatestVersion))
        {
            return true;
        }

        knownLatestVersion = null;
        return false;
    }
}

#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
