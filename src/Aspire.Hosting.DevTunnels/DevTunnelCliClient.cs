// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Text.Json;

namespace Aspire.Hosting.DevTunnels;

internal class DevTunnelCliClient(DevTunnelCli cli) : IDevTunnelClient
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private readonly DevTunnelCli _cli = cli ?? throw new ArgumentNullException(nameof(cli));

    public Task<DevTunnelPort> CreateOrUpdatePortAsync(string tunnelId, int portNumber, DevTunnelPortOptions options, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<DevTunnelStatus> CreateOrUpdateTunnelAsync(string tunnelId, DevTunnelOptions options, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<DevTunnelStatus> GetTunnelAsync(string tunnelId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<UserLoginStatus> GetUserLoginStatusAsync(CancellationToken cancellationToken = default)
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var exitCode = await _cli.UserStatusAsync(stdout, stderr, cancellationToken).ConfigureAwait(false);

        if (exitCode != 0)
        {
            var error = stderr.ToString().Trim();
            throw new DistributedApplicationException($"Failed to get user login status. Exit code {exitCode}: {error}");
        }

        var output = stdout.ToString().Trim();
        var userLoginStatus = JsonSerializer.Deserialize<UserLoginStatus>(output, _jsonOptions)
            ?? throw new DistributedApplicationException("Failed to parse user login status.");

        return userLoginStatus;
    }

    public async Task<UserLoginStatus> UserLoginAsync(LoginProvider provider, CancellationToken cancellationToken = default)
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = provider switch
        {
            LoginProvider.Microsoft => await _cli.UserLoginMicrosoftAsync(stdout, stderr, cancellationToken).ConfigureAwait(false),
            LoginProvider.GitHub => await _cli.UserLoginGitHubAsync(stdout, stderr, cancellationToken).ConfigureAwait(false),
            _ => throw new ArgumentException("Unsupported provider. Supported providers are 'microsoft' and 'github'.", nameof(provider)),
        };

        if (exitCode != 0)
        {
            var error = stderr.ToString().Trim();
            throw new DistributedApplicationException($"Failed to perform user login. Exit code {exitCode}: {error}");
        }

        return await GetUserLoginStatusAsync(cancellationToken).ConfigureAwait(false);
    }
}
