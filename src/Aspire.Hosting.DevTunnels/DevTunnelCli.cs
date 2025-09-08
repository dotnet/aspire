// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.DevTunnels;

internal sealed class DevTunnelCli
{
    public const int ResourceConflictsWithExistingExitCode = 1;
    public const int ResourceNotFoundExitCode = 2;

    private readonly string _cliPath;

    public static string GetCliPath(IConfiguration configuration) => configuration["ASPIRE_DEVTUNNEL_CLI_PATH"] ?? "devtunnel";

    public DevTunnelCli(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("CLI path must be provided", nameof(filePath));
        }

        _cliPath = filePath;
    }

    public string CliPath => _cliPath;

    public Task<int> UserLoginMicrosoftAsync(CancellationToken cancellationToken = default)
        => RunAsync(["user", "login", "--entra", "--json"], null, null, useShellExecute: true, cancellationToken);

    public Task<int> UserLoginGitHubAsync(CancellationToken cancellationToken = default)
        => RunAsync(["user", "login", "--github", "--json"], null, null, useShellExecute: true, cancellationToken);

    public Task<int> UserLogoutAsync(TextWriter? outputWriter = null, TextWriter? errorWriter = null, string? provider = null, CancellationToken cancellationToken = default)
        => RunAsync(DevTunnelCliArgBuilderExtensions.BuildArgs(static list =>
        {
            list.Add("user");
            list.Add("logout");
        })
        .AddIfNotNull("--provider", provider)
        .ToArray(), outputWriter, errorWriter, cancellationToken);

    public Task<int> UserStatusAsync(TextWriter? outputWriter = null, TextWriter? errorWriter = null, CancellationToken cancellationToken = default)
        => RunAsync(["user", "show", "--json"], outputWriter, errorWriter, cancellationToken);

    public Task<int> CreateTunnelAsync(
        string? tunnelId = null,
        DevTunnelOptions? options = null,
        TextWriter? outputWriter = null,
        TextWriter? errorWriter = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new DevTunnelOptions();
        return RunAsync(DevTunnelCliArgBuilderExtensions.BuildArgs(static list =>
        {
            list.Add("create");
        })
        .AddIfNotNull(tunnelId)
        .AddIfNotNull("--description", options.Description)
        .AddIfTrue("--allow-anonymous", options.AllowAnonymous)
        .AddLabels("--labels", options.Labels)
        .AddIfTrue("--json", true)
        .ToArray(), outputWriter, errorWriter, cancellationToken);
    }

    public Task<int> UpdateTunnelAsync(
        string tunnelId,
        DevTunnelOptions? options = null,
        TextWriter? outputWriter = null,
        TextWriter? errorWriter = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new DevTunnelOptions();
        return RunAsync(DevTunnelCliArgBuilderExtensions.BuildArgs(list =>
        {
            list.Add("update");
            list.Add(tunnelId);
        })
        .AddIfNotNull("--description", options.Description)
        .AddLabels("--add-labels", options.Labels)
        .AddIfTrue("--json", true)
        .ToArray(), outputWriter, errorWriter, cancellationToken);
    }

    public Task<int> DeleteTunnelAsync(string tunnelId, TextWriter? outputWriter = null, TextWriter? errorWriter = null, CancellationToken cancellationToken = default)
        => RunAsync(["delete", tunnelId, "--json"], outputWriter, errorWriter, cancellationToken);

    public Task<int> ShowTunnelAsync(string tunnelId, TextWriter? outputWriter = null, TextWriter? errorWriter = null, CancellationToken cancellationToken = default)
        => RunAsync(["show", tunnelId, "--json"], outputWriter, errorWriter, cancellationToken);

    public Task<int> CreatePortAsync(
        string tunnelId,
        int portNumber,
        DevTunnelPortOptions? options = null,
        TextWriter? outputWriter = null,
        TextWriter? errorWriter = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new DevTunnelPortOptions();
        return RunAsync(DevTunnelCliArgBuilderExtensions.BuildArgs(list =>
        {
            list.Add("port");
            list.Add("create");
            list.Add(tunnelId);
        })
        .Add("--port-number", portNumber.ToString(CultureInfo.InvariantCulture))
        .AddIfNotNull("--protocol", options.Protocol)
        .AddLabels("--labels", options.Labels)
        .AddIfTrue("--json", true)
        .ToArray(), outputWriter, errorWriter, cancellationToken);
    }

    public Task<int> UpdatePortAsync(
        string tunnelId,
        int portNumber,
        DevTunnelPortOptions? options = null,
        TextWriter? outputWriter = null,
        TextWriter? errorWriter = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new DevTunnelPortOptions();
        return RunAsync(DevTunnelCliArgBuilderExtensions.BuildArgs(list =>
        {
            list.Add("port");
            list.Add("update");
            list.Add(tunnelId);
        })
        .Add("--port-number", portNumber.ToString(CultureInfo.InvariantCulture))
        .AddIfNotNull("--description", options.Description)
        .AddLabels("--add-labels", options.Labels)
        .AddIfTrue("--json", true)
        .ToArray(), outputWriter, errorWriter, cancellationToken);
    }

    public async Task<int> CreateOrUpdatePortAsync(
        string tunnelId,
        int portNumber,
        DevTunnelPortOptions? options = null,
        TextWriter? outputWriter = null,
        TextWriter? errorWriter = null,
        CancellationToken cancellationToken = default)
    {
        var exitCode = await UpdatePortAsync(tunnelId, portNumber, options, outputWriter, errorWriter, cancellationToken).ConfigureAwait(false);
        if (exitCode == ResourceNotFoundExitCode)
        {
            // Port does not exist, create it
            return await CreatePortAsync(tunnelId, portNumber, options, outputWriter, errorWriter, cancellationToken).ConfigureAwait(false);
        }
        return exitCode;
    }

    public Task<int> DeletePortAsync(string tunnelId, int portNumber, TextWriter? outputWriter = null, TextWriter? errorWriter = null, CancellationToken cancellationToken = default)
        => RunAsync(["port", "delete", "--tunnel-id", tunnelId, "--port-number", portNumber.ToString(CultureInfo.InvariantCulture)], outputWriter, errorWriter, cancellationToken);

    public Task<int> ShowPortAsync(string tunnelId, int portNumber, TextWriter? outputWriter = null, TextWriter? errorWriter = null, CancellationToken cancellationToken = default)
        => RunAsync(["port", "show", "--tunnel-id", tunnelId, "--port-number", portNumber.ToString(CultureInfo.InvariantCulture)], outputWriter, errorWriter, cancellationToken);

    public Task<int> SetAnonymousAccessAsync(string tunnelId, bool allowAnonymous, TextWriter? outputWriter = null, TextWriter? errorWriter = null, CancellationToken cancellationToken = default)
        => UpdateTunnelAsync(tunnelId, options: new DevTunnelOptions { AllowAnonymous = allowAnonymous }, outputWriter: outputWriter, errorWriter: errorWriter, cancellationToken: cancellationToken);

    public Task<int> SetTenantAccessAsync(string tunnelId, string? tenant, TextWriter? outputWriter = null, TextWriter? errorWriter = null, CancellationToken cancellationToken = default)
        => UpdateTunnelAsync(tunnelId, options: new DevTunnelOptions { Tenant = tenant }, outputWriter: outputWriter, errorWriter: errorWriter, cancellationToken: cancellationToken);

    public Task<int> SetOrganizationAccessAsync(string tunnelId, string? organization, TextWriter? outputWriter = null, TextWriter? errorWriter = null, CancellationToken cancellationToken = default)
        => UpdateTunnelAsync(tunnelId, options: new DevTunnelOptions { Organization = organization }, outputWriter: outputWriter, errorWriter: errorWriter, cancellationToken: cancellationToken);

    public async Task<bool> UserIsLoggedInAsync(TextWriter? outputWriter = null, TextWriter? errorWriter = null, CancellationToken cancellationToken = default)
    {
        var outputBuilder = new StringBuilder();
        var exitCode = await RunAsync(
            (isError, line) =>
            {
                if (!isError)
                {
                    outputBuilder.AppendLine(line);
                    outputWriter?.WriteLine(line);
                }
                else
                {
                    errorWriter?.WriteLine(line);
                }
            },
            ["user", "show", "--json"],
            cancellationToken).ConfigureAwait(false);

        var output = outputBuilder.ToString();

        if (exitCode != 0)
        {
            return false;
        }

        try
        {
            var jsonOutput = JsonDocument.Parse(output.ToString());
            return jsonOutput.RootElement.GetProperty("status").GetString() != "Not logged in";
        }
        catch (JsonException ex)
        {
            errorWriter?.WriteLine($"Failed to get user login status: {ex.Message}");
            return false;
        }
    }

    private Task<int> RunAsync(string[] args, TextWriter? outputWriter = null, TextWriter? errorWriter = null, CancellationToken cancellationToken = default)
        => RunAsync(args, outputWriter, errorWriter, useShellExecute: false, cancellationToken);

    private Task<int> RunAsync(string[] args, TextWriter? outputWriter = null, TextWriter? errorWriter = null, bool useShellExecute = false, CancellationToken cancellationToken = default)
    {
        return RunAsync((isError, line) =>
        {
            if (isError)
            {
                errorWriter?.WriteLine(line);
            }
            else
            {
                outputWriter?.WriteLine(line);
            }
        }, args, useShellExecute, cancellationToken);
    }

    private async Task<int> RunAsync(Action<bool, string> onOutput, string[] args, CancellationToken cancellationToken = default)
        => await RunAsync(onOutput, args, useShellExecute: false, cancellationToken).ConfigureAwait(false);

    private async Task<int> RunAsync(Action<bool, string> onOutput, string[] args, bool useShellExecute = false, CancellationToken cancellationToken = default)
    {
        using var process = new Process
        {
            StartInfo = BuildStartInfo(args, useShellExecute),
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

            if (!useShellExecute)
            {
                stdoutTask = PumpAsync(process.StandardOutput, line => onOutput(false, line), cancellationToken);
                stderrTask = PumpAsync(process.StandardError, line => onOutput(true, line), cancellationToken);
            }

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
            if (!useShellExecute)
            {
                await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);
            }

            return process.ExitCode;
        }
        finally
        {
            if (!useShellExecute)
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
    }

    private ProcessStartInfo BuildStartInfo(IEnumerable<string> args, bool useShellExecute = false)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _cliPath,
            UseShellExecute = useShellExecute,
            RedirectStandardOutput = !useShellExecute,
            RedirectStandardError = !useShellExecute,
            RedirectStandardInput = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        // Prefer ArgumentList to avoid quoting issues.
        foreach (var a in args)
        {
            if (string.IsNullOrWhiteSpace(a))
            {
                continue;
            }

            psi.ArgumentList.Add(a);
        }

        // Ensure consistent encoding on Windows terminals
        if (!useShellExecute && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            psi.StandardOutputEncoding = Encoding.UTF8;
            psi.StandardErrorEncoding = Encoding.UTF8;
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

// TODO: Rewrite this helper to be more ergnomic, e.g. new ArgsBuilder().Add(...).AddIf(...)
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

    internal static List<string> AddIfNotNull(this List<string> list, string? name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            list.Add(name);
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
    internal static List<string> AddLabels(this List<string> list, string name, List<string>? labels)
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
            list.Add(name);
            list.Add(joined);
        }

        return list;
    }
}
