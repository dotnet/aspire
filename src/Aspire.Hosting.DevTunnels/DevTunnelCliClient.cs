// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.DevTunnels;

internal sealed class DevTunnelCliClient(IConfiguration configuration) : IDevTunnelClient
{
    private readonly int _maxCliAttempts = configuration.GetValue<int?>("ASPIRE_DEVTUNNEL_CLI_MAX_ATTEMPTS") ?? 3;
    private readonly TimeSpan _cliRetryOnErrorDelay = TimeSpan.FromSeconds(2);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private readonly DevTunnelCli _cli = new(DevTunnelCli.GetCliPath(configuration));

    public async Task<DevTunnelStatus> CreateTunnelAsync(string tunnelId, DevTunnelOptions options, ILogger? logger = default, CancellationToken cancellationToken = default)
    {
        var attempts = 0;
        var exitCode = 0;
        string? error = null;

        while (attempts < _maxCliAttempts)
        {
            logger?.LogTrace("Creating dev tunnel '{TunnelId}' with options: {Options}", tunnelId, options.ToLoggerString());
            if (attempts++ > 1)
            {
                logger?.LogTrace("Attempt {Attempt} of {MaxAttempts} to create dev tunnel '{TunnelId}'", attempts, _maxCliAttempts, tunnelId);
            }
            (var tunnel, exitCode, error) = await CallCliAsJsonAsync<DevTunnelStatus>(
                (stdout, stderr, log, ct) => _cli.CreateTunnelAsync(tunnelId, options, stdout, stderr, log, ct),
                "tunnel",
                logger, cancellationToken).ConfigureAwait(false);

            if (exitCode == 0 && tunnel is not null)
            {
                logger?.LogTrace("Dev tunnel '{TunnelId}' created successfully.", tunnelId);
                return tunnel;
            }

            if (exitCode == DevTunnelCli.ResourceConflictsWithExistingExitCode)
            {
                // Delete the tunnel and try again
                logger?.LogTrace("Dev tunnel '{TunnelId}' already exists, deleting and trying again.", tunnelId);
                (var result, exitCode, error) = await CallCliAsJsonAsync<DevTunnelDeleteResult>(
                    (stdout, stderr, log, ct) => _cli.DeleteTunnelAsync(tunnelId, stdout, stderr, log, ct),
                    logger, cancellationToken).ConfigureAwait(false);
                if (exitCode == 0)
                {
                    logger?.LogTrace("Dev tunnel '{TunnelId}' deleted successfully.", tunnelId);
                    continue;
                }
            }

            logger?.LogError("Failed to create dev tunnel '{TunnelId}' (attempt {Attempt} of {MaxAttempts}). Exit code {ExitCode}: {Error}", tunnelId, attempts, _maxCliAttempts, exitCode, error);
            if (attempts < _maxCliAttempts)
            {
                logger?.LogTrace("Waiting {WaitSeconds} seconds before retrying to create dev tunnel '{TunnelId}'", _cliRetryOnErrorDelay.TotalSeconds, tunnelId);
                await Task.Delay(_cliRetryOnErrorDelay, cancellationToken).ConfigureAwait(false);
            }
        }

        throw new DistributedApplicationException($"Failed to create dev tunnel '{tunnelId}' after {attempts} attempts. Exit code {exitCode}: {error}");
    }

    public async Task<DevTunnelStatus> GetTunnelAsync(string tunnelId, ILogger? logger = default, CancellationToken cancellationToken = default)
    {
        logger?.LogTrace("Getting details for dev tunnel '{TunnelId}'.", tunnelId);
        var (tunnel, exitCode, error) = await CallCliAsJsonAsync<DevTunnelStatus>(
            (stdout, stderr, log, ct) => _cli.ShowTunnelAsync(tunnelId, stdout, stderr, log, ct),
            "tunnel",
            logger, cancellationToken).ConfigureAwait(false);
        return tunnel ?? throw new DistributedApplicationException($"Failed to get dev tunnel '{tunnelId}'. Exit code {exitCode}: {error}");
    }

    public async Task<DevTunnelPortStatus> CreatePortAsync(string tunnelId, int portNumber, DevTunnelPortOptions options, ILogger? logger = default, CancellationToken cancellationToken = default)
    {
        var attempts = 0;
        var exitCode = 0;
        string? error = null;
        DevTunnelPortStatus? port = null;

        while (attempts < _maxCliAttempts)
        {
            logger?.LogTrace("Creating port '{PortNumber}' on dev tunnel '{TunnelId}' with options: {Options}", portNumber, tunnelId, options.ToLoggerString());
            if (attempts++ > 1)
            {
                logger?.LogTrace("Attempt {Attempt} of {MaxAttempts} to create port '{PortNumber}' on dev tunnel '{TunnelId}'", attempts, _maxCliAttempts, portNumber, tunnelId);
            }

            (port, exitCode, error) = await CallCliAsJsonAsync<DevTunnelPortStatus>(
                (outWriter, errWriter, log, ct) => _cli.CreatePortAsync(tunnelId, portNumber, options, outWriter, errWriter, log, ct),
                logger, cancellationToken).ConfigureAwait(false);

            if (exitCode == 0 && port is not null)
            {
                if (options.AllowAnonymous.HasValue)
                {
                    // AllowAnonymous=true: anonymous=true, deny=false
                    // AllowAnonymous=false: anonymous=true, deny=true
                    var anonymous = true;
                    var deny = !options.AllowAnonymous.Value;
                    if (deny)
                    {
                        logger?.LogTrace("Denying anonymous access for port '{PortNumber}' on dev tunnel '{TunnelId}'.", portNumber, tunnelId);
                    }
                    else
                    {
                        logger?.LogTrace("Allowing anonymous access for port '{PortNumber}' on dev tunnel '{TunnelId}'.", portNumber, tunnelId);
                    }
                    (var result, exitCode, error) = await CallCliAsJsonAsync<DevTunnelAccessStatus>(
                        (stdout, stderr, log, ct) => _cli.CreateAccessAsync(tunnelId, portNumber, anonymous, deny, stdout, stderr, log, ct),
                        logger, cancellationToken).ConfigureAwait(false);
                }
                if (exitCode == 0)
                {
                    logger?.LogTrace("Dev tunnel '{TunnelId}' created successfully.", tunnelId);
                    return port;
                }
            }
            else if (exitCode == DevTunnelCli.ResourceConflictsWithExistingExitCode)
            {
                logger?.LogTrace("Port '{PortNumber}' already exists on dev tunnel '{TunnelId}', deleting and trying again.", portNumber, tunnelId);
                (var deleteResult, exitCode, error) = await CallCliAsJsonAsync<DevTunnelDeleteResult>(
                    (stdout, stderr, log, ct) => _cli.DeletePortAsync(tunnelId, portNumber, stdout, stderr, log, ct),
                    logger, cancellationToken).ConfigureAwait(false);
                if (exitCode == 0)
                {
                    logger?.LogTrace("Deleted existing port '{PortNumber}' on dev tunnel '{TunnelId}'.", portNumber, tunnelId);
                    continue; // Retry create
                }
            }

            logger?.LogError("Failed to create port '{PortNumber}' for dev tunnel '{TunnelId}' (attempt {Attempt} of {MaxAttempts}). Exit code {ExitCode}: {Error}", portNumber, tunnelId, attempts, _maxCliAttempts, exitCode, error);
            if (attempts < _maxCliAttempts)
            {
                logger?.LogTrace("Waiting {WaitSeconds} seconds before retrying to create port '{PortNumber}' on dev tunnel '{TunnelId}'", _cliRetryOnErrorDelay.TotalSeconds, portNumber, tunnelId);
                await Task.Delay(_cliRetryOnErrorDelay, cancellationToken).ConfigureAwait(false);
            }
        }

        throw new DistributedApplicationException($"Failed to create port '{portNumber}' for tunnel '{tunnelId}' after {attempts} attempts. Exit code {exitCode}: {error}");
    }

    public async Task<DevTunnelAccessStatus> GetAccessAsync(string tunnelId, int? portNumber = null, ILogger? logger = default, CancellationToken cancellationToken = default)
    {
        logger?.LogTrace("Getting access details for {PortInfo}dev tunnel '{TunnelId}'.", tunnelId, portNumber.HasValue ? $"port '{portNumber}' on " : string.Empty);
        var (access, exitCode, error) = await CallCliAsJsonAsync<DevTunnelAccessStatus>(
            (stdout, stderr, log, ct) => _cli.ListAccessAsync(tunnelId, portNumber, stdout, stderr, log, ct),
            logger, cancellationToken).ConfigureAwait(false);
        return access ?? throw new DistributedApplicationException($"Failed to get access details for '{tunnelId}'{(portNumber.HasValue ? $" port {portNumber}" : "")}. Exit code {exitCode}: {error}");
    }

    public async Task<UserLoginStatus> GetUserLoginStatusAsync(ILogger? logger = default, CancellationToken cancellationToken = default)
    {
        logger?.LogTrace("Getting dev tunnel user login status.");
        var (login, exitCode, error) = await CallCliAsJsonAsync<UserLoginStatus>(
            _cli.UserStatusAsync,
            logger, cancellationToken).ConfigureAwait(false);
        return login ?? throw new DistributedApplicationException($"Failed to get user login status. Exit code {exitCode}: {error}");
    }

    public async Task<UserLoginStatus> UserLoginAsync(LoginProvider provider, ILogger? logger = default, CancellationToken cancellationToken = default)
    {
        logger?.LogTrace("Logging in to dev tunnel service using {LoginProvider}.", provider);
        var exitCode = provider switch
        {
            LoginProvider.Microsoft => await _cli.UserLoginMicrosoftAsync(logger, cancellationToken).ConfigureAwait(false),
            LoginProvider.GitHub => await _cli.UserLoginGitHubAsync(logger, cancellationToken).ConfigureAwait(false),
            _ => throw new ArgumentException("Unsupported provider. Supported providers are 'microsoft' and 'github'.", nameof(provider)),
        };

        if (exitCode == 0)
        {
            // Login succeeded, get the login status
            return await GetUserLoginStatusAsync(logger, cancellationToken).ConfigureAwait(false);
        }

        throw new DistributedApplicationException($"Failed to perform user login. Process finished with exit code: {exitCode}");
    }

    private async Task<(T? Result, int ExitCode, string? Error)> CallCliAsJsonAsync<T>(Func<TextWriter, TextWriter, ILogger?, CancellationToken, Task<int>> cliCall, ILogger? logger = default, CancellationToken cancellationToken = default)
    {
        return await CallCliAsJsonAsync<T>(cliCall, propertyName: null, logger, cancellationToken).ConfigureAwait(false);
    }

    private async Task<(T? Result, int ExitCode, string? Error)> CallCliAsJsonAsync<T>(Func<TextWriter, TextWriter, ILogger?, CancellationToken, Task<int>> cliCall, string? propertyName, ILogger? logger = default, CancellationToken cancellationToken = default)
    {
        // PERF: Could pool these writers
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cliCall(stdout, stderr, logger, cancellationToken).ConfigureAwait(false);

        if (exitCode != 0)
        {
            var error = stderr.ToString().Trim();
            logger?.LogError("CLI call returned non-zero exit code '{ExitCode}'. stderr output:\n{Error}", exitCode, error);
            return (default, exitCode, error);
        }

        var output = stdout.ToString().Trim();
        logger?.LogTrace("CLI call output:\n{Output}", output);
        try
        {
            if (!string.IsNullOrEmpty(propertyName))
            {
                output = JsonDocument.Parse(output).RootElement.GetProperty(propertyName).GetRawText();
                logger?.LogTrace("Extracted JSON property '{PropertyName}':\n{Output}", propertyName, output);
            }
            var result = JsonSerializer.Deserialize<T>(output, _jsonOptions);
            logger?.LogTrace("JSON output successfully deserialized to '{TypeName}' instance", typeof(T).Name);
            return (result, 0, default);
        }
        catch (JsonException ex)
        {
            logger?.LogError(ex, "Failed to parse JSON output into type '{TypeName}'", typeof(T).Name);
            throw new DistributedApplicationException($"Failed to parse JSON output into type '{typeof(T).Name}':\n{output}", ex);
        }
    }

    private record DevTunnelDeleteResult(string DeletedTunnel);
}
