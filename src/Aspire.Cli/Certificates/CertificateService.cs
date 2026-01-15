// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Certificates;

/// <summary>
/// The result of ensuring certificates are trusted.
/// </summary>
internal sealed class EnsureCertificatesTrustedResult
{
    /// <summary>
    /// Gets the environment variables that should be set for the AppHost process
    /// to ensure certificates are properly trusted.
    /// </summary>
    public required IDictionary<string, string> EnvironmentVariables { get; init; }
}

internal interface ICertificateService
{
    Task<EnsureCertificatesTrustedResult> EnsureCertificatesTrustedAsync(IDotNetCliRunner runner, CancellationToken cancellationToken);
}

internal sealed class CertificateService(IInteractionService interactionService, AspireCliTelemetry telemetry) : ICertificateService
{
    private const string SslCertDirEnvVar = "SSL_CERT_DIR";
    private static readonly string s_devCertsTrustPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".aspnet",
        "dev-certs",
        "trust");

    public async Task<EnsureCertificatesTrustedResult> EnsureCertificatesTrustedAsync(IDotNetCliRunner runner, CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity(nameof(EnsureCertificatesTrustedAsync), ActivityKind.Client);

        var environmentVariables = new Dictionary<string, string>();
        var ensureCertificateCollector = new OutputCollector();

        // First, try to use the machine-readable check (available in .NET 10 SDK)
        var machineReadableResult = await TryCheckMachineReadableAsync(runner, ensureCertificateCollector, cancellationToken);

        if (machineReadableResult is not null)
        {
            await HandleMachineReadableTrustAsync(runner, machineReadableResult, ensureCertificateCollector, environmentVariables, cancellationToken);
        }
        else
        {
            // Fall back to the old behavior if machine-readable check isn't available
            await HandleLegacyTrustAsync(runner, ensureCertificateCollector, cancellationToken);
        }

        return new EnsureCertificatesTrustedResult
        {
            EnvironmentVariables = environmentVariables
        };
    }

    private async Task<CertificateTrustResult?> TryCheckMachineReadableAsync(
        IDotNetCliRunner runner,
        OutputCollector collector,
        CancellationToken cancellationToken)
    {
        var options = new DotNetCliRunnerInvocationOptions
        {
            StandardOutputCallback = collector.AppendOutput,
            StandardErrorCallback = collector.AppendError,
        };

        var (exitCode, result) = await interactionService.ShowStatusAsync(
            $":locked_with_key: {InteractionServiceStrings.CheckingCertificates}",
            async () => await runner.CheckHttpCertificateMachineReadableAsync(options, cancellationToken));

        // If the command failed with exit code that indicates it's not supported, return null
        // Exit code 1 with null result indicates the command isn't available
        if (result is null)
        {
            return null;
        }

        return result;
    }

    private async Task HandleMachineReadableTrustAsync(
        IDotNetCliRunner runner,
        CertificateTrustResult trustResult,
        OutputCollector collector,
        Dictionary<string, string> environmentVariables,
        CancellationToken cancellationToken)
    {
        // If fully trusted, nothing more to do
        if (trustResult.IsFullyTrusted)
        {
            return;
        }

        // If not trusted at all, run the trust operation
        if (trustResult.IsNotTrusted)
        {
            var options = new DotNetCliRunnerInvocationOptions
            {
                StandardOutputCallback = collector.AppendOutput,
                StandardErrorCallback = collector.AppendError,
            };

            var trustExitCode = await interactionService.ShowStatusAsync(
                $":locked_with_key: {InteractionServiceStrings.TrustingCertificates}",
                () => runner.TrustHttpCertificateAsync(options, cancellationToken));

            if (trustExitCode != 0)
            {
                interactionService.DisplayLines(collector.GetLines());
                interactionService.DisplayMessage("warning", string.Format(CultureInfo.CurrentCulture, ErrorStrings.CertificatesMayNotBeFullyTrusted, trustExitCode));
            }

            // Re-check trust status after trust operation
            var recheckOptions = new DotNetCliRunnerInvocationOptions
            {
                StandardOutputCallback = collector.AppendOutput,
                StandardErrorCallback = collector.AppendError,
            };

            var (_, recheckResult) = await runner.CheckHttpCertificateMachineReadableAsync(recheckOptions, cancellationToken);
            if (recheckResult is not null)
            {
                trustResult = recheckResult;
            }
        }

        // If partially trusted (either initially or after trust), configure SSL_CERT_DIR on Linux
        if (trustResult.IsPartiallyTrusted && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            ConfigureSslCertDir(environmentVariables);
        }
    }

    private async Task HandleLegacyTrustAsync(
        IDotNetCliRunner runner,
        OutputCollector collector,
        CancellationToken cancellationToken)
    {
        var checkOptions = new DotNetCliRunnerInvocationOptions
        {
            StandardOutputCallback = collector.AppendOutput,
            StandardErrorCallback = collector.AppendError,
        };

        var checkExitCode = await interactionService.ShowStatusAsync(
            $":locked_with_key: {InteractionServiceStrings.CheckingCertificates}",
            async () => await runner.CheckHttpCertificateAsync(checkOptions, cancellationToken));

        if (checkExitCode != 0)
        {
            var trustOptions = new DotNetCliRunnerInvocationOptions
            {
                StandardOutputCallback = collector.AppendOutput,
                StandardErrorCallback = collector.AppendError,
            };

            var trustExitCode = await interactionService.ShowStatusAsync(
                $":locked_with_key: {InteractionServiceStrings.TrustingCertificates}",
                () => runner.TrustHttpCertificateAsync(trustOptions, cancellationToken));

            if (trustExitCode != 0)
            {
                interactionService.DisplayLines(collector.GetLines());
                interactionService.DisplayMessage("warning", string.Format(CultureInfo.CurrentCulture, ErrorStrings.CertificatesMayNotBeFullyTrusted, trustExitCode));
            }
        }
    }

    private static void ConfigureSslCertDir(Dictionary<string, string> environmentVariables)
    {
        // Get the current SSL_CERT_DIR value (if any)
        var currentSslCertDir = Environment.GetEnvironmentVariable(SslCertDirEnvVar);

        // Check if the dev-certs trust path is already included
        if (!string.IsNullOrEmpty(currentSslCertDir))
        {
            var paths = currentSslCertDir.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
            if (paths.Any(p => string.Equals(p.TrimEnd(Path.DirectorySeparatorChar), s_devCertsTrustPath.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)))
            {
                // Already included, nothing to do
                return;
            }

            // Append the dev-certs trust path to the existing value
            environmentVariables[SslCertDirEnvVar] = $"{currentSslCertDir}{Path.PathSeparator}{s_devCertsTrustPath}";
        }
        else
        {
            // Set the dev-certs trust path as the only value
            // On Linux, we also need to include the default system certificate directories
            // Common locations: /etc/ssl/certs, /etc/pki/tls/certs
            var systemCertDirs = new List<string>();

            if (Directory.Exists("/etc/ssl/certs"))
            {
                systemCertDirs.Add("/etc/ssl/certs");
            }

            if (Directory.Exists("/etc/pki/tls/certs"))
            {
                systemCertDirs.Add("/etc/pki/tls/certs");
            }

            systemCertDirs.Add(s_devCertsTrustPath);

            environmentVariables[SslCertDirEnvVar] = string.Join(Path.PathSeparator, systemCertDirs);
        }
    }
}

public sealed class CertificateServiceException(string message) : Exception(message)
{

}
