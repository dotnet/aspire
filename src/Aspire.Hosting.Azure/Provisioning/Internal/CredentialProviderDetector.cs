// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure.Provisioning.Internal;

/// <summary>
/// Detects available Azure credential providers in the user's environment.
/// </summary>
internal sealed class CredentialProviderDetector(ILogger<CredentialProviderDetector> logger)
{
    private readonly ILogger<CredentialProviderDetector> _logger = logger;

    /// <summary>
    /// Detects which credential providers are available in the current environment.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of available credential provider names.</returns>
    public async Task<List<string>> DetectAvailableProvidersAsync(CancellationToken cancellationToken = default)
    {
        var availableProviders = new List<string>();

        // Check each credential provider in parallel for better performance
        var detectionTasks = new[]
        {
            Task.Run(() => IsAzureCliAvailableAsync(cancellationToken), cancellationToken),
            Task.Run(() => IsVisualStudioAvailableAsync(cancellationToken), cancellationToken),
            Task.Run(() => IsVisualStudioCodeAvailableAsync(cancellationToken), cancellationToken),
            Task.Run(() => IsAzurePowerShellAvailableAsync(cancellationToken), cancellationToken),
            Task.Run(() => IsAzureDeveloperCliAvailableAsync(cancellationToken), cancellationToken)
        };

        var results = await Task.WhenAll(detectionTasks).ConfigureAwait(false);

        var providers = new[] { "AzureCli", "VisualStudio", "VisualStudioCode", "AzurePowerShell", "AzureDeveloperCli" };
        for (int i = 0; i < results.Length; i++)
        {
            if (results[i])
            {
                availableProviders.Add(providers[i]);
                _logger.LogDebug("Detected available credential provider: {Provider}", providers[i]);
            }
        }

        // Always include InteractiveBrowser as a fallback option
        availableProviders.Add("InteractiveBrowser");
        _logger.LogDebug("Added InteractiveBrowser as fallback credential provider");

        return availableProviders;
    }

    private async Task<bool> IsAzureCliAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Check if 'az' command exists and user is logged in
            var azPath = FindExecutableInPath("az");
            if (string.IsNullOrEmpty(azPath))
            {
                return false;
            }

            // Try to get account info to verify login status
            var result = await RunCommandAsync(azPath, "account show --output none", cancellationToken).ConfigureAwait(false);
            return result.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to detect Azure CLI availability");
            return false;
        }
    }

    private async Task<bool> IsVisualStudioAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Try to create a VisualStudioCredential and get a token to verify it works
            var credential = new VisualStudioCredential(new VisualStudioCredentialOptions
            {
                // Use a short timeout to avoid hanging
                ProcessTimeout = TimeSpan.FromSeconds(5)
            });

            // Try to get a token for Azure Resource Manager
            var tokenRequest = new TokenRequestContext(["https://management.azure.com/.default"]);
            var token = await credential.GetTokenAsync(tokenRequest, cancellationToken).ConfigureAwait(false);
            return !string.IsNullOrEmpty(token.Token);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to detect Visual Studio credential availability");
            return false;
        }
    }

    private async Task<bool> IsVisualStudioCodeAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Try to create a VisualStudioCodeCredential and get a token to verify it works
            var credential = new VisualStudioCodeCredential();

            // Try to get a token for Azure Resource Manager with a timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            
            var tokenRequest = new TokenRequestContext(["https://management.azure.com/.default"]);
            var token = await credential.GetTokenAsync(tokenRequest, cts.Token).ConfigureAwait(false);
            return !string.IsNullOrEmpty(token.Token);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to detect Visual Studio Code credential availability");
            return false;
        }
    }

    private async Task<bool> IsAzurePowerShellAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Check if PowerShell is available and Az module is installed
            var pwshPath = FindExecutableInPath("pwsh") ?? FindExecutableInPath("powershell");
            if (string.IsNullOrEmpty(pwshPath))
            {
                return false;
            }

            // Try to check if Az.Accounts module is available and user is logged in
            var command = "Get-Module -ListAvailable Az.Accounts -ErrorAction SilentlyContinue | Select-Object -First 1";
            var result = await RunCommandAsync(pwshPath, $"-NoProfile -NonInteractive -Command \"{command}\"", cancellationToken).ConfigureAwait(false);
            
            if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.Output))
            {
                return false;
            }

            // Check if user is logged in
            var loginCheckCommand = "Get-AzContext -ErrorAction SilentlyContinue | Select-Object -First 1";
            var loginResult = await RunCommandAsync(pwshPath, $"-NoProfile -NonInteractive -Command \"{loginCheckCommand}\"", cancellationToken).ConfigureAwait(false);
            return loginResult.ExitCode == 0 && !string.IsNullOrWhiteSpace(loginResult.Output);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to detect Azure PowerShell availability");
            return false;
        }
    }

    private async Task<bool> IsAzureDeveloperCliAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Check if 'azd' command exists and user is logged in
            var azdPath = FindExecutableInPath("azd");
            if (string.IsNullOrEmpty(azdPath))
            {
                return false;
            }

            // Try to check login status
            var result = await RunCommandAsync(azdPath, "auth login --check-status", cancellationToken).ConfigureAwait(false);
            return result.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to detect Azure Developer CLI availability");
            return false;
        }
    }

    private static string? FindExecutableInPath(string executable)
    {
        // Add extension for Windows
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !executable.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            executable += ".exe";
        }

        var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? [];

        foreach (var path in paths)
        {
            try
            {
                var fullPath = Path.Combine(path, executable);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
            catch
            {
                // Ignore errors when checking paths
            }
        }

        return null;
    }

    private static async Task<(int ExitCode, string Output)> RunCommandAsync(string executable, string arguments, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        var outputBuilder = new System.Text.StringBuilder();
        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();

        using var cancellationRegistration = cancellationToken.Register(() =>
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
                // Ignore errors when killing process
            }
        });

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        return (process.ExitCode, outputBuilder.ToString());
    }
}
