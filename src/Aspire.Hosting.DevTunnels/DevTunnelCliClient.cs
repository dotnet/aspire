// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.DevTunnels;

internal sealed class DevTunnelCliClient(IConfiguration configuration) : IDevTunnelClient
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private readonly DevTunnelCli _cli = new(DevTunnelCli.GetCliPath(configuration));

    public async Task<DevTunnelStatus> CreateOrUpdateTunnelAsync(string tunnelId, DevTunnelOptions options, CancellationToken cancellationToken = default)
    {
        var (tunnel, exitCode, error) = await CallCliAsJsonAsync<DevTunnelStatus>(
            (stdout, stderr, ct) => _cli.CreateTunnelAsync(tunnelId, options, stdout, stderr, ct),
            "tunnel",
            cancellationToken).ConfigureAwait(false);

        if (exitCode == DevTunnelCli.ResourceConflictsWithExistingExitCode)
        {
            // Tunnel already exists

            // Reset the access controls to match the updated options
            (var access, exitCode, error) = await CallCliAsJsonAsync<DevTunnelAccessStatus>(
                (stdout, stderr, ct) => _cli.ResetAccessAsync(tunnelId, /* port */ null, stdout, stderr, ct),
                cancellationToken).ConfigureAwait(false);

            if (options.AllowAnonymous)
            {
                var anonymous = true;
                var deny = false;
                (access, exitCode, error) = await CallCliAsJsonAsync<DevTunnelAccessStatus>(
                    (stdout, stderr, ct) => _cli.CreateAccessAsync(tunnelId, /* port */ null, anonymous, deny, stdout, stderr, ct),
                    cancellationToken).ConfigureAwait(false);
            }

            if (exitCode == 0)
            {
                // Do the update
                (tunnel, exitCode, error) = await CallCliAsJsonAsync<DevTunnelStatus>(
                    (stdout, stderr, ct) => _cli.UpdateTunnelAsync(tunnelId, options, stdout, stderr, ct),
                    "tunnel",
                    cancellationToken).ConfigureAwait(false);
            }
        }

        return tunnel ?? throw new DistributedApplicationException($"Failed to create tunnel '{tunnelId}'. Exit code {exitCode}: {error}");
    }

    public async Task<DevTunnelStatus> GetTunnelAsync(string tunnelId, CancellationToken cancellationToken = default)
    {
        var (tunnel, exitCode, error) = await CallCliAsJsonAsync<DevTunnelStatus>(
            (stdout, stderr, ct) => _cli.ShowTunnelAsync(tunnelId, stdout, stderr, ct),
            "tunnel",
            cancellationToken).ConfigureAwait(false);
        return tunnel ?? throw new DistributedApplicationException($"Failed to get tunnel '{tunnelId}'. Exit code {exitCode}: {error}");
    }

    public async Task<DevTunnelPortStatus> CreateOrUpdatePortAsync(string tunnelId, int portNumber, DevTunnelPortOptions options, CancellationToken cancellationToken = default)
    {
        var (port, exitCode, error) = await CallCliAsJsonAsync<DevTunnelPortStatus>(
                (outWriter, errWriter, ct) => _cli.CreatePortAsync(tunnelId, portNumber, options, outWriter, errWriter, ct),
                cancellationToken).ConfigureAwait(false);

        if (exitCode == 0)
        {
            if (options.AllowAnonymous.HasValue)
            {
                // AllowAnonymous=true: anonymous=true, deny=false
                // AllowAnonymous=false: anonymous=true, deny=true
                var anonymous = true;
                var deny = !options.AllowAnonymous.Value;
                (var access, exitCode, error) = await CallCliAsJsonAsync<DevTunnelAccessStatus>(
                    (stdout, stderr, ct) => _cli.CreateAccessAsync(tunnelId, portNumber, anonymous, deny, stdout, stderr, ct),
                    cancellationToken).ConfigureAwait(false);
            }
        }
        else if (exitCode == DevTunnelCli.ResourceConflictsWithExistingExitCode)
        {
            // Port already exists

            // Reset the access controls to match the updated options
            (var access, exitCode, error) = await CallCliAsJsonAsync<DevTunnelAccessStatus>(
                (stdout, stderr, ct) => _cli.ResetAccessAsync(tunnelId, portNumber, stdout, stderr, ct),
                cancellationToken).ConfigureAwait(false);

            if (options.AllowAnonymous.HasValue)
            {
                // AllowAnonymous=true: anonymous=true, deny=false
                // AllowAnonymous=false: anonymous=true, deny=true
                var anonymous = true;
                var deny = !options.AllowAnonymous.Value;
                (access, exitCode, error) = await CallCliAsJsonAsync<DevTunnelAccessStatus>(
                    (stdout, stderr, ct) => _cli.CreateAccessAsync(tunnelId, portNumber, anonymous, deny, stdout, stderr, ct),
                    cancellationToken).ConfigureAwait(false);
            }

            if (exitCode == 0)
            {
                (port, exitCode, error) = await CallCliAsJsonAsync<DevTunnelPortStatus>(
                    (stdout, stderr, ct) => _cli.UpdatePortAsync(tunnelId, portNumber, options, stdout, stderr, ct),
                    "port",
                    cancellationToken).ConfigureAwait(false);
            }
        }

        return port ?? throw new DistributedApplicationException($"Failed to create port '{portNumber}' for tunnel '{tunnelId}'. Exit code {exitCode}: {error}");
    }

    public async Task<DevTunnelAccessStatus> GetAccessAsync(string tunnelId, int? portNumber = null, CancellationToken cancellationToken = default)
    {
        var (access, exitCode, error) = await CallCliAsJsonAsync<DevTunnelAccessStatus>(
            (stdout, stderr, ct) => _cli.ListAccessAsync(tunnelId, portNumber, stdout, stderr, ct),
            cancellationToken).ConfigureAwait(false);
        return access ?? throw new DistributedApplicationException($"Failed to get access details for '{tunnelId}'{(portNumber.HasValue ? $" port {portNumber}" : "")}. Exit code {exitCode}: {error}");
    }

    public async Task<UserLoginStatus> GetUserLoginStatusAsync(CancellationToken cancellationToken = default)
    {
        var (login, exitCode, error) = await CallCliAsJsonAsync<UserLoginStatus>(
            _cli.UserStatusAsync,
            cancellationToken).ConfigureAwait(false);
        return login ?? throw new DistributedApplicationException($"Failed to get user login status. Exit code {exitCode}: {error}");
    }

    public async Task<UserLoginStatus> UserLoginAsync(LoginProvider provider, CancellationToken cancellationToken = default)
    {
        var exitCode = provider switch
        {
            LoginProvider.Microsoft => await _cli.UserLoginMicrosoftAsync(cancellationToken).ConfigureAwait(false),
            LoginProvider.GitHub => await _cli.UserLoginGitHubAsync(cancellationToken).ConfigureAwait(false),
            _ => throw new ArgumentException("Unsupported provider. Supported providers are 'microsoft' and 'github'.", nameof(provider)),
        };

        if (exitCode == 0)
        {
            // Login succeeded, get the login status
            return await GetUserLoginStatusAsync(cancellationToken).ConfigureAwait(false);
        }

        throw new DistributedApplicationException($"Failed to perform user login. Process finished with exit code: {exitCode}");
    }

    private async Task<(T? Result, int ExitCode, string? Error)> CallCliAsJsonAsync<T>(Func<TextWriter, TextWriter, CancellationToken, Task<int>> cliCall, CancellationToken cancellationToken = default)
    {
        return await CallCliAsJsonAsync<T>(cliCall, propertyName: null, cancellationToken).ConfigureAwait(false);
    }

    private async Task<(T? Result, int ExitCode, string? Error)> CallCliAsJsonAsync<T>(Func<TextWriter, TextWriter, CancellationToken, Task<int>> cliCall, string? propertyName, CancellationToken cancellationToken = default)
    {
        // PERF: Could pool these writers
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cliCall(stdout, stderr, cancellationToken).ConfigureAwait(false);

        if (exitCode != 0)
        {
            var error = stderr.ToString().Trim();
            return (default, exitCode, error);
        }

        var output = stdout.ToString().Trim();
        try
        {
            var result = propertyName switch
            {
                { Length: > 0 } => JsonSerializer.Deserialize<T>(JsonDocument.Parse(output).RootElement.GetProperty(propertyName).GetRawText(), _jsonOptions),
                _ => JsonSerializer.Deserialize<T>(output, _jsonOptions),
            };
            return (result, 0, default);
        }
        catch (JsonException ex)
        {
            throw new DistributedApplicationException($"Failed to parse JSON output into type '{typeof(T).Name}':\n{output}", ex);
        }
    }
}
