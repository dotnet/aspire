// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Aspire.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.SecretManager.Tools.Internal;
using Semver;

namespace Aspire.Hosting.VersionChecking;

internal sealed class VersionCheckService : BackgroundService
{
    private static readonly TimeSpan s_checkInterval = TimeSpan.FromDays(2);

    internal const string LastCheckDateKey = "Aspire:VersionCheck:LastCheckDate";
    internal const string KnownLatestVersionKey = "Aspire:VersionCheck:KnownLatestVersion";
    internal const string IgnoreVersionKey = "Aspire:VersionCheck:IgnoreVersion";

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

        _appHostVersion = PackageUpdateHelpers.GetCurrentPackageVersion();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_interactionService.IsAvailable || _executionContext.IsPublishMode || _configuration.GetBool(KnownConfigNames.VersionCheckDisabled, defaultValue: false))
        {
            // Don't check version if there is no way to prompt that information to the user.
            // Or app is being run during a publish.
            return;
        }

        if (_appHostVersion == null)
        {
            _logger.LogDebug("App host version is not available, skipping version check.");
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

    private async Task CheckForLatestAsync(CancellationToken cancellationToken)
    {
        Debug.Assert(_appHostVersion != null);

        var now = _timeProvider.GetUtcNow();
        var checkForLatestVersion = true;
        if (_configuration[LastCheckDateKey] is string checkDateString &&
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
            var appHostDirectory = _configuration["AppHost:Directory"]!;

            SecretsStore.TrySetUserSecret(_options.Assembly, LastCheckDateKey, now.ToString("o", CultureInfo.InvariantCulture));
            var packages = await _versionFetcher.TryFetchVersionsAsync(appHostDirectory, cancellationToken).ConfigureAwait(false);

            latestVersion = PackageUpdateHelpers.GetNewerVersion(_appHostVersion, packages);
        }

        if (TryGetConfigVersion(KnownLatestVersionKey, out var storedKnownLatestVersion))
        {
            if (latestVersion == null)
            {
                // Use the known latest version if we can't check for the latest version.
                latestVersion = storedKnownLatestVersion;
            }
        }

        if (latestVersion == null || IsVersionGreaterOrEqual(_appHostVersion, latestVersion))
        {
            // App host version is up to date or the latest version is unknown.
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
            SecretsStore.TrySetUserSecret(_options.Assembly, KnownLatestVersionKey, latestVersion.ToString());
        }

        var result = await _interactionService.PromptMessageBarAsync(
            title: "Update now",
            message: $"Aspire {latestVersion} is available.",
            options: new MessageBarInteractionOptions
            {
                LinkText = "Upgrade instructions",
                LinkUrl = "https://aka.ms/dotnet/aspire/update-latest",
                PrimaryButtonText = "Ignore"
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        // True when the user clicked the primary button (Ignore).
        if (result.Data)
        {
            _logger.LogDebug("User chose to ignore version {Version}.", latestVersion);
            SecretsStore.TrySetUserSecret(_options.Assembly, IgnoreVersionKey, latestVersion.ToString());
        }
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
