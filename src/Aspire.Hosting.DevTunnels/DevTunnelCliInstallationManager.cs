// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
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

#pragma warning disable ASPIREINTERACTION001 // Interaction service is experimental.
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
}
