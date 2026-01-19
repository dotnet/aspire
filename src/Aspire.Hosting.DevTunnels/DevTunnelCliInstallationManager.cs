// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.DevTunnels;

internal sealed class DevTunnelCliInstallationManager : RequiredCommandValidator
{
    private readonly IDevTunnelClient _devTunnelClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;
    private readonly Version _minSupportedVersion;
    private string? _resolvedCommandPath;
    private bool _suppressInstaller;

#pragma warning disable ASPIREINTERACTION001 // Interaction service is experimental.
    private readonly IInteractionService _interactionService;

    public DevTunnelCliInstallationManager(
        IDevTunnelClient devTunnelClient,
        IConfiguration configuration,
        IInteractionService interactionService,
        ILogger<DevTunnelCliInstallationManager> logger)
        : this(devTunnelClient, configuration, interactionService, logger, DevTunnelCli.MinimumSupportedVersion)
    {

    }

    public DevTunnelCliInstallationManager(
        IDevTunnelClient devTunnelClient,
        IConfiguration configuration,
        IInteractionService interactionService,
        ILogger<DevTunnelCliInstallationManager> logger,
        Version minSupportedVersion)
        : base(interactionService, logger)
#pragma warning restore ASPIREINTERACTION001
    {
        _devTunnelClient = devTunnelClient ?? throw new ArgumentNullException(nameof(devTunnelClient));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _interactionService = interactionService ?? throw new ArgumentNullException(nameof(interactionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _minSupportedVersion = minSupportedVersion ?? throw new ArgumentNullException(nameof(minSupportedVersion));
    }

    /// <summary>
    /// Gets the resolved full path to the devtunnel CLI after a successful validation, otherwise <c>null</c>.
    /// </summary>
    public string? ResolvedCommandPath => _resolvedCommandPath;

    /// <summary>
    /// Gets a value indicating whether the CLI was found (after calling <see cref="EnsureInstalledAsync"/>).
    /// </summary>
    public bool IsInstalled => _resolvedCommandPath is not null;

    /// <summary>
    /// Suppresses the installer prompt. When set to true, the installer prompt will not be shown.
    /// </summary>
    public bool SuppressInstaller
    {
        get => _suppressInstaller;
        set => _suppressInstaller = value;
    }

    /// <summary>
    /// Ensures the devtunnel CLI is installed/available. This method is safe for concurrent callers;
    /// only one validation will run at a time.
    /// </summary>
    /// <throws cref="DistributedApplicationException">Thrown if the devtunnel CLI is not found.</throws>
    public Task EnsureInstalledAsync(CancellationToken cancellationToken = default) => RunAsync(cancellationToken);

    protected override string GetCommandPath() => DevTunnelCli.GetCliPath(_configuration);

    protected internal override async Task<(bool IsValid, string? ValidationMessage)> OnResolvedAsync(string resolvedCommandPath, CancellationToken cancellationToken)
    {
        // Verify the version is supported
        var version = await _devTunnelClient.GetVersionAsync(_logger, cancellationToken).ConfigureAwait(false);
        if (version < _minSupportedVersion)
        {
            // Try to offer upgrade if not suppressed
            if (!_suppressInstaller && _interactionService.IsAvailable)
            {
                var upgraded = await TryUpgradeCliAsync(version, cancellationToken).ConfigureAwait(false);
                if (upgraded)
                {
                    // Re-check version after upgrade
                    version = await _devTunnelClient.GetVersionAsync(_logger, cancellationToken).ConfigureAwait(false);
                    if (version >= _minSupportedVersion)
                    {
                        return (true, null);
                    }
                }
            }

            return (false, string.Format(CultureInfo.CurrentCulture, Resources.MessageStrings.DevtunnelCliVersionNotSupported, version, _minSupportedVersion));
        }
        return (true, null);
    }

    protected override Task OnValidatedAsync(string resolvedCommandPath, CancellationToken cancellationToken)
    {
        _resolvedCommandPath = resolvedCommandPath;
        return Task.CompletedTask;
    }

    protected override string? GetHelpLink() => "https://learn.microsoft.com/azure/developer/dev-tunnels/get-started#install";

#pragma warning disable ASPIREINTERACTION001
    protected override async Task<string?> OnCommandNotFoundAsync(string command, CancellationToken cancellationToken)
    {
        if (_suppressInstaller)
        {
            _logger.LogDebug("Installer is suppressed, skipping install prompt");
            return null;
        }

        if (!_interactionService.IsAvailable)
        {
            _logger.LogDebug("Interaction service not available, skipping install prompt");
            return null;
        }

        return await TryInstallCliAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<string?> TryInstallCliAsync(CancellationToken cancellationToken)
    {
        // Check if package manager is available
        var (isAvailable, missingMessage) = DevTunnelCliInstaller.CheckPackageManagerAvailable();
        if (!isAvailable)
        {
            _logger.LogWarning("Package manager not available: {Message}", missingMessage);

            // Show notification about missing prerequisite
            await _interactionService.PromptNotificationAsync(
                Resources.MessageStrings.DevtunnelCliInstallPromptTitle,
                missingMessage!,
                new NotificationInteractionOptions
                {
                    Intent = MessageIntent.Warning,
                    LinkText = Resources.MessageStrings.InstallationInstructions,
                    LinkUrl = GetHelpLink(),
                    ShowDismiss = true
                },
                cancellationToken).ConfigureAwait(false);

            return null;
        }

        // Prompt user for confirmation
        var installCommand = DevTunnelCliInstaller.GetInstallCommand();
        var message = $"{Resources.MessageStrings.DevtunnelCliNotFound}\n\nCommand: {installCommand}";

        var result = await _interactionService.PromptConfirmationAsync(
            Resources.MessageStrings.DevtunnelCliInstallPromptTitle,
            message,
            new MessageBoxInteractionOptions
            {
                Intent = MessageIntent.Confirmation,
                PrimaryButtonText = Resources.MessageStrings.DevtunnelCliInstallButtonInstall,
                SecondaryButtonText = Resources.MessageStrings.DevtunnelCliInstallButtonCancel,
                ShowSecondaryButton = true,
                ShowDismiss = false
            },
            cancellationToken).ConfigureAwait(false);

        if (result.Canceled || !result.Data)
        {
            _logger.LogInformation("User cancelled devtunnel CLI installation");

            // Show a notification that installation was cancelled
            await _interactionService.PromptNotificationAsync(
                "Dev tunnels",
                Resources.MessageStrings.DevtunnelCliInstallCancelled,
                new NotificationInteractionOptions
                {
                    Intent = MessageIntent.Information,
                    LinkText = Resources.MessageStrings.InstallationInstructions,
                    LinkUrl = GetHelpLink(),
                    ShowDismiss = true
                },
                cancellationToken).ConfigureAwait(false);

            return null;
        }

        // User confirmed, proceed with installation
        _logger.LogInformation("User confirmed devtunnel CLI installation");

        var installer = new DevTunnelCliInstaller(_logger);
        var (installResult, errorMessage) = await installer.InstallAsync(cancellationToken).ConfigureAwait(false);

        if (installResult == DevTunnelCliInstaller.InstallResult.Success)
        {
            _logger.LogInformation("devtunnel CLI installed successfully");

            await _interactionService.PromptNotificationAsync(
                "Dev tunnels",
                Resources.MessageStrings.DevtunnelCliInstallSuccess,
                new NotificationInteractionOptions
                {
                    Intent = MessageIntent.Success,
                    ShowDismiss = true
                },
                cancellationToken).ConfigureAwait(false);

            // Return the new path - re-resolve after installation
            return PathLookupHelper.FindFullPathFromPath("devtunnel");
        }

        // Installation failed
        var failedMessage = string.Format(CultureInfo.CurrentCulture, Resources.MessageStrings.DevtunnelCliInstallFailed, errorMessage ?? "Unknown error");
        _logger.LogWarning("devtunnel CLI installation failed: {Message}", failedMessage);

        await _interactionService.PromptNotificationAsync(
            "Dev tunnels",
            failedMessage,
            new NotificationInteractionOptions
            {
                Intent = MessageIntent.Error,
                LinkText = Resources.MessageStrings.InstallationInstructions,
                LinkUrl = GetHelpLink(),
                ShowDismiss = true
            },
            cancellationToken).ConfigureAwait(false);

        return null;
    }

    private async Task<bool> TryUpgradeCliAsync(Version currentVersion, CancellationToken cancellationToken)
    {
        // Check if package manager is available
        var (isAvailable, missingMessage) = DevTunnelCliInstaller.CheckPackageManagerAvailable();
        if (!isAvailable)
        {
            _logger.LogWarning("Package manager not available for upgrade: {Message}", missingMessage);

            // Show notification about unsupported version
            var unsupportedMessage = string.Format(
                CultureInfo.CurrentCulture,
                Resources.MessageStrings.DevtunnelCliVersionNotSupported,
                currentVersion,
                _minSupportedVersion);

            await _interactionService.PromptNotificationAsync(
                Resources.MessageStrings.DevtunnelCliInstallPromptTitle,
                $"{unsupportedMessage}\n\n{missingMessage}",
                new NotificationInteractionOptions
                {
                    Intent = MessageIntent.Warning,
                    LinkText = Resources.MessageStrings.InstallationInstructions,
                    LinkUrl = GetHelpLink(),
                    ShowDismiss = true
                },
                cancellationToken).ConfigureAwait(false);

            return false;
        }

        // Prompt user for confirmation to upgrade
        var installCommand = DevTunnelCliInstaller.GetInstallCommand();
        var message = string.Format(
            CultureInfo.CurrentCulture,
            Resources.MessageStrings.DevtunnelCliVersionNotSupportedInstall,
            currentVersion,
            _minSupportedVersion) + $"\n\nCommand: {installCommand}";

        var result = await _interactionService.PromptConfirmationAsync(
            Resources.MessageStrings.DevtunnelCliInstallPromptTitle,
            message,
            new MessageBoxInteractionOptions
            {
                Intent = MessageIntent.Confirmation,
                PrimaryButtonText = Resources.MessageStrings.DevtunnelCliInstallButtonInstall,
                SecondaryButtonText = Resources.MessageStrings.DevtunnelCliInstallButtonCancel,
                ShowSecondaryButton = true,
                ShowDismiss = false
            },
            cancellationToken).ConfigureAwait(false);

        if (result.Canceled || !result.Data)
        {
            _logger.LogInformation("User cancelled devtunnel CLI upgrade");
            return false;
        }

        // User confirmed, proceed with upgrade
        _logger.LogInformation("User confirmed devtunnel CLI upgrade from version {CurrentVersion}", currentVersion);

        var installer = new DevTunnelCliInstaller(_logger);
        var (installResult, errorMessage) = await installer.InstallAsync(cancellationToken).ConfigureAwait(false);

        if (installResult == DevTunnelCliInstaller.InstallResult.Success)
        {
            _logger.LogInformation("devtunnel CLI upgraded successfully");

            await _interactionService.PromptNotificationAsync(
                "Dev tunnels",
                Resources.MessageStrings.DevtunnelCliInstallSuccess,
                new NotificationInteractionOptions
                {
                    Intent = MessageIntent.Success,
                    ShowDismiss = true
                },
                cancellationToken).ConfigureAwait(false);

            return true;
        }

        // Upgrade failed
        var failedMessage = string.Format(CultureInfo.CurrentCulture, Resources.MessageStrings.DevtunnelCliInstallFailed, errorMessage ?? "Unknown error");
        _logger.LogWarning("devtunnel CLI upgrade failed: {Message}", failedMessage);

        await _interactionService.PromptNotificationAsync(
            "Dev tunnels",
            failedMessage,
            new NotificationInteractionOptions
            {
                Intent = MessageIntent.Error,
                LinkText = Resources.MessageStrings.InstallationInstructions,
                LinkUrl = GetHelpLink(),
                ShowDismiss = true
            },
            cancellationToken).ConfigureAwait(false);

        return false;
    }
#pragma warning restore ASPIREINTERACTION001
}
