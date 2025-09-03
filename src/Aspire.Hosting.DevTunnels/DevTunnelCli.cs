// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.DevTunnels;

/// <summary>
/// Thin wrapper around the Dev Tunnels CLI ("devtunnel") that exposes common commands as async methods.
/// - All methods stream stdout/stderr to the provided <see cref="ILogger"/>.
/// - The constructor requires the absolute path to the CLI executable.
/// - Focuses on persistent tunnels. Temporary/ephemeral tunnels are intentionally not supported.
///
/// CLI reference: https://learn.microsoft.com/azure/developer/dev-tunnels/cli-commands
/// </summary>
internal sealed class DevTunnelCli
{
    //private const int ResourceConflictsWithExistingExitCode = 1;
    private const int ResourceNotFoundExitCode = 2;

    private readonly string _cliPath;

    public DevTunnelCli() : this("D:\\src\\devtunnel.exe") { }

    /// <summary>
    /// Create a new manager instance.
    /// </summary>
    /// <param name="filePath">Path to the devtunnel CLI executable.</param>
    public DevTunnelCli(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("CLI path must be provided", nameof(filePath));
        }

        _cliPath = filePath;
    }

    /// <summary>
    /// Log in to the dev tunnels service with the specified provider.
    /// </summary>
    /// <param name="logger">Logger to stream CLI output to.</param>
    /// <param name="provider">Auth provider, for example: "microsoft", "entra", or "github". If null, CLI default UI is used.</param>
    /// <param name="tenant">Optional Microsoft Entra tenant ID or domain (when applicable).</param>
    /// <param name="organization">Optional GitHub organization (when applicable).</param>
    /// <param name="cancellationToken">Cancellation token to cancel the login process.</param>
    /// <returns>CLI exit code.</returns>
#pragma warning disable IDE0060 // Remove unused parameter
    internal Task<int> UserLoginAsync(ILogger logger, string? provider = null, string? tenant = null, string? organization = null, CancellationToken cancellationToken = default)
#pragma warning restore IDE0060 // Remove unused parameter
        => RunAsync(logger, cancellationToken, DevTunnelCliArgBuilderExtensions.BuildArgs(static list =>
        {
            list.Add("user");
            list.Add("login");
        })
        //.AddIfNotNull("--provider", provider)
        .AddIfNotNull("--tenant", tenant)
        .AddIfNotNull("--organization", organization)
        .ToArray());

    /// <summary>
    /// Log in using a Microsoft (MSA or Entra) account with optional tenant hint.
    /// Convenience overload for <see cref="UserLoginAsync(ILogger, string?, string?, string?, CancellationToken)"/>.
    /// </summary>
    public Task<int> UserLoginMicrosoftAsync(ILogger logger, string? tenant = null, CancellationToken cancellationToken = default)
        => UserLoginAsync(logger, provider: "microsoft", tenant: tenant, organization: null, cancellationToken);

    /// <summary>
    /// Log in using a GitHub account with optional organization hint.
    /// Convenience overload for <see cref="UserLoginAsync(ILogger, string?, string?, string?, CancellationToken)"/>.
    /// </summary>
    public Task<int> UserLoginGitHubAsync(ILogger logger, string? organization = null, CancellationToken cancellationToken = default)
        => UserLoginAsync(logger, provider: "github", tenant: null, organization: organization, cancellationToken);

    /// <summary>
    /// Log out of the dev tunnels service. Optionally restrict to a given provider.
    /// </summary>
    public Task<int> UserLogoutAsync(ILogger logger, string? provider = null, CancellationToken cancellationToken = default)
        => RunAsync(logger, cancellationToken, DevTunnelCliArgBuilderExtensions.BuildArgs(static list =>
        {
            list.Add("user");
            list.Add("logout");
        })
        .AddIfNotNull("--provider", provider)
        .ToArray());

    /// <summary>
    /// Show current login status (active user contexts cached by the CLI).
    /// </summary>
    public Task<int> UserStatusAsync(ILogger logger, CancellationToken cancellationToken = default)
        => RunAsync(logger, cancellationToken, "user", "show");

    /// <summary>
    /// Create a persistent tunnel.
    /// </summary>
    /// <param name="logger">Logger to stream CLI output to.</param>
    /// <param name="tunnelId">Optional explicit tunnel ID to create; otherwise the service assigns one.</param>
    /// <param name="name">Optional friendly name for the tunnel.</param>
    /// <param name="options">Tunnel options that map to CLI arguments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>CLI exit code.</returns>
    public Task<int> CreateTunnelAsync(
        ILogger logger,
        string? tunnelId = null,
        string? name = null,
        DevTunnelOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new DevTunnelOptions();
        return RunAsync(logger, cancellationToken, DevTunnelCliArgBuilderExtensions.BuildArgs(static list => list.Add("create"))
            .AddIfNotNull("--tunnel-id", tunnelId)
            .AddIfNotNull("--name", name)
            .AddIfNotNull("--description", options.Description)
            .AddIfTrue("--allow-anonymous", options.AllowAnonymous)
            .AddIfNotNull("--domain", options.Domain)
            .AddIfNotNull("--tenant", options.Tenant)
            .AddIfNotNull("--organization", options.Organization)
            .AddLabels(options.Labels)
            .ToArray());
    }

    /// <summary>
    /// Update an existing tunnel's metadata or access.
    /// </summary>
    /// <param name="logger">Logger to stream CLI output to.</param>
    /// <param name="tunnelId">The tunnel ID to update.</param>
    /// <param name="name">Optional new friendly name.</param>
    /// <param name="options">Tunnel options that map to CLI arguments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>CLI exit code.</returns>
    public Task<int> UpdateTunnelAsync(
        ILogger logger,
        string tunnelId,
        string? name = null,
        DevTunnelOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new DevTunnelOptions();
        return RunAsync(logger, cancellationToken, DevTunnelCliArgBuilderExtensions.BuildArgs(static list =>
        {
            list.Add("update");
        })
        .Add("--tunnel-id", tunnelId)
        .AddIfNotNull("--name", name)
        .AddIfNotNull("--description", options.Description)
        .AddIfTrue("--allow-anonymous", options.AllowAnonymous)
        .AddIfNotNull("--domain", options.Domain)
        .AddIfNotNull("--tenant", options.Tenant)
        .AddIfNotNull("--organization", options.Organization)
        .AddLabels(options.Labels)
        .ToArray());
    }

    public async Task<int> CreateOrUpdateTunnelAsync(
        ILogger logger,
        string tunnelId,
        string? name = null,
        DevTunnelOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        var exitCode = await UpdateTunnelAsync(logger, tunnelId, name, options, cancellationToken).ConfigureAwait(false);
        if (exitCode == ResourceNotFoundExitCode)
        {
            // Tunnel does not exist, create it
            return await UpdateTunnelAsync(logger, tunnelId, name, options, cancellationToken).ConfigureAwait(false);
        }
        return exitCode;
    }

    /// <summary>
    /// Delete a tunnel.
    /// </summary>
    public Task<int> DeleteTunnelAsync(ILogger logger, string tunnelId, CancellationToken cancellationToken = default)
        => RunAsync(logger, cancellationToken, "delete", "--tunnel-id", tunnelId);

    /// <summary>
    /// Show tunnel details.
    /// </summary>
    public Task<int> ShowTunnelAsync(ILogger logger, string tunnelId, CancellationToken cancellationToken = default)
        => RunAsync(logger, cancellationToken, "show", "--tunnel-id", tunnelId);

    

    /// <summary>
    /// Create a port on a persistent tunnel.
    /// </summary>
    /// <param name="logger">Logger to stream CLI output to.</param>
    /// <param name="tunnelId">The persistent tunnel ID to add a port to.</param>
    /// <param name="portNumber">The TCP port number to expose (1-65535).</param>
    /// <param name="options">Port options that map to CLI arguments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>CLI exit code.</returns>
    public Task<int> CreatePortAsync(
        ILogger logger,
        string tunnelId,
        int portNumber,
        DevTunnelPortOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new DevTunnelPortOptions();
        return RunAsync(logger, cancellationToken, DevTunnelCliArgBuilderExtensions.BuildArgs(static list =>
        {
            list.Add("port");
            list.Add("create");
        })
        .Add("--tunnel-id", tunnelId)
        .Add("--port-number", portNumber.ToString(CultureInfo.InvariantCulture))
        .AddIfNotNull("--protocol", options.Protocol)
        .AddIfNotNull("--name", options.Name)
        .AddIfTrue("--require-authentication", options.RequireAuthentication)
        .AddIfNotNull("--host-header", options.ForwardHostHeader)
        .AddIfNotNull("--path-prefix", options.PathPrefix)
        .AddLabels(options.Labels)
        .ToArray());
    }

    /// <summary>
    /// Update a port on a persistent tunnel.
    /// </summary>
    /// <param name="logger">Logger to stream CLI output to.</param>
    /// <param name="tunnelId">The persistent tunnel ID.</param>
    /// <param name="portNumber">The port number to update.</param>
    /// <param name="options">Port options that map to CLI arguments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>CLI exit code.</returns>
    public Task<int> UpdatePortAsync(
        ILogger logger,
        string tunnelId,
        int portNumber,
        DevTunnelPortOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new DevTunnelPortOptions();
        return RunAsync(logger, cancellationToken, DevTunnelCliArgBuilderExtensions.BuildArgs(static list =>
        {
            list.Add("port");
            list.Add("update");
        })
        .Add("--tunnel-id", tunnelId)
        .Add("--port-number", portNumber.ToString(CultureInfo.InvariantCulture))
        .AddIfNotNull("--protocol", options.Protocol)
        .AddIfNotNull("--name", options.Name)
        .AddIfTrue("--require-authentication", options.RequireAuthentication)
        .AddIfNotNull("--host-header", options.ForwardHostHeader)
        .AddIfNotNull("--path-prefix", options.PathPrefix)
        .AddLabels(options.Labels)
        .ToArray());
    }

    public async Task<int> CreateOrUpdatePortAsync(ILogger logger,
        string tunnelId,
        int portNumber,
        DevTunnelPortOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var exitCode = await UpdatePortAsync(logger, tunnelId, portNumber, options, cancellationToken).ConfigureAwait(false);
        if (exitCode == ResourceNotFoundExitCode)
        {
            // Port does not exist, create it
            return await CreatePortAsync(logger, tunnelId, portNumber, options, cancellationToken).ConfigureAwait(false);
        }
        return exitCode;
    }

    /// <summary>
    /// Delete a tunnel port.
    /// </summary>
    public Task<int> DeletePortAsync(ILogger logger, string tunnelId, int portNumber, CancellationToken cancellationToken = default)
        => RunAsync(logger, cancellationToken, "port", "delete", "--tunnel-id", tunnelId, "--port-number", portNumber.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Show port details.
    /// </summary>
    public Task<int> ShowPortAsync(ILogger logger, string tunnelId, int portNumber, CancellationToken cancellationToken = default)
        => RunAsync(logger, cancellationToken, "port", "show", "--tunnel-id", tunnelId, "--port-number", portNumber.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Grant or revoke anonymous access on a tunnel by updating its access policy.
    /// </summary>
    public Task<int> SetAnonymousAccessAsync(ILogger logger, string tunnelId, bool allowAnonymous, CancellationToken cancellationToken = default)
        => UpdateTunnelAsync(logger, tunnelId, options: new DevTunnelOptions { AllowAnonymous = allowAnonymous }, cancellationToken: cancellationToken);

    /// <summary>
    /// Extend tunnel access to a specific Microsoft Entra tenant (or clear by passing null).
    /// </summary>
    public Task<int> SetTenantAccessAsync(ILogger logger, string tunnelId, string? tenant, CancellationToken cancellationToken = default)
        => UpdateTunnelAsync(logger, tunnelId, options: new DevTunnelOptions { Tenant = tenant }, cancellationToken: cancellationToken);

    /// <summary>
    /// Extend tunnel access to members of a GitHub organization (or clear by passing null).
    /// </summary>
    public Task<int> SetOrganizationAccessAsync(ILogger logger, string tunnelId, string? organization, CancellationToken cancellationToken = default)
        => UpdateTunnelAsync(logger, tunnelId, options: new DevTunnelOptions { Organization = organization }, cancellationToken: cancellationToken);

    public async Task<bool> UserIsLoggedInAsync(ILogger logger, CancellationToken cancellationToken = default)
    {
        var outputBuilder = new StringBuilder();
        var exitCode = await RunAsync(
            (isError, line) =>
            {
                if (!isError)
                {
                    outputBuilder.AppendLine(line);
                }
            },
            cancellationToken,
            "user", "show", "--json").ConfigureAwait(false);

        var output = outputBuilder.ToString();

        if (exitCode != 0)
        {
            logger.LogError("Failed to get user login status: ExitCode={ExitCode}, Output={Output}", exitCode, output);
            return false;
        }

        try
        {
            var jsonOutput = JsonDocument.Parse(output.ToString());
            return jsonOutput.RootElement.GetProperty("status").GetString() != "Not logged in";
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to get user login status: {Message}", ex.Message);
            return false;
        }
    }

    private Task<int> RunAsync(ILogger logger, CancellationToken cancellationToken, params string[] args)
    {
        return RunAsync((isError, line) =>
        {
            if (isError)
            {
                logger.LogError("{Line}", line);
            }
            else
            {
                logger.LogInformation("{Line", line);
            }
        }, cancellationToken, args);
    }

    private async Task<int> RunAsync(Action<bool, string> onOutput, CancellationToken cancellationToken, params string[] args)
    {
        using var process = new Process
        {
            StartInfo = BuildStartInfo(args),
            EnableRaisingEvents = true
        };

        var stdoutTask = Task.CompletedTask;
        var stderrTask = Task.CompletedTask;

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException("Failed to start devtunnel process.");
            }

            stdoutTask = PumpAsync(process.StandardOutput, line => onOutput(false, line), cancellationToken);
            stderrTask = PumpAsync(process.StandardError, line => onOutput(true, line), cancellationToken);

            using var ctr = cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // ignored
                }
            });

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);

            return process.ExitCode;
        }
        finally
        {
            try
            {
                await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);
            }
            catch
            {
                // ignored
            }
        }
    }

    private ProcessStartInfo BuildStartInfo(IEnumerable<string> args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _cliPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = false,
            CreateNoWindow = true,
        };

        // Prefer ArgumentList to avoid quoting issues.
        foreach (var a in args)
        {
            psi.ArgumentList.Add(a);
        }

        // Ensure consistent encoding on Windows terminals
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            psi.StandardOutputEncoding = System.Text.Encoding.UTF8;
            psi.StandardErrorEncoding = System.Text.Encoding.UTF8;
        }

        return psi;
    }

    private static async Task PumpAsync(StreamReader reader, Action<string> onLine, CancellationToken cancellationToken)
    {
        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                break;
            }
            if (line.Length > 0)
            {
                onLine(line);
            }
        }
    }
}

internal static class DevTunnelCliArgBuilderExtensions
{
    internal static List<string> BuildArgs(Action<List<string>> addBase)
    {
        var list = new List<string>();
        addBase(list);
        return list;
    }

    internal static List<string> Add(this List<string> list, string name, string value)
    {
        list.Add(name);
        list.Add(value);
        return list;
    }

    internal static List<string> AddIfNotNull(this List<string> list, string name, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            list.Add(name);
            list.Add(value);
        }
        return list;
    }

    internal static List<string> AddIfTrue(this List<string> list, string name, bool? condition)
    {
        if (condition == true)
        {
            list.Add(name);
        }
        return list;
    }

    internal static List<string> AddIfFalse(this List<string> list, string name, bool? condition)
    {
        if (condition == false)
        {
            list.Add(name);
        }
        return list;
    }

    /// <summary>
    /// Adds labels using a single option in the form: --labels "label1 label2".
    /// The value is a space-separated list of label strings.
    /// </summary>
    internal static List<string> AddLabels(this List<string> list, List<string>? labels)
    {
        if (labels is null || labels.Count == 0)
        {
            return list;
        }

        // Build a single space-separated string of labels.
        // Assumes label strings themselves do not contain spaces.
        var tokens = labels.Where(l => !string.IsNullOrWhiteSpace(l));
        var joined = string.Join(' ', tokens);
        if (!string.IsNullOrWhiteSpace(joined))
        {
            list.Add("--labels");
            list.Add(joined);
        }

        return list;
    }
}
