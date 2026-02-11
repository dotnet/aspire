// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using Aspire.Cli.DotNet;
using Aspire.Cli.Layout;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Certificates;

/// <summary>
/// Certificate tool runner for bundle mode that invokes aspire-managed dev-certs
/// instead of the global dotnet SDK's dev-certs command.
/// </summary>
internal sealed class BundleCertificateToolRunner(
    ILayoutDiscovery layoutDiscovery,
    ILogger<BundleCertificateToolRunner> logger) : ICertificateToolRunner
{
    public async Task<(int ExitCode, CertificateTrustResult? Result)> CheckHttpCertificateMachineReadableAsync(
        DotNetCliRunnerInvocationOptions options,
        CancellationToken cancellationToken)
    {
        var managedPath = GetManagedPath();

        var outputBuilder = new StringBuilder();

        var arguments = new[] { "dev-certs", "--check-trust-machine-readable" };

        logger.LogDebug("Running: {ManagedPath} {Args}", managedPath, string.Join(" ", arguments));

        var (exitCode, output, error) = await LayoutProcessRunner.RunAsync(
            managedPath,
            arguments,
            ct: cancellationToken);

        // Forward output/error to callers
        if (!string.IsNullOrEmpty(output))
        {
            outputBuilder.Append(output);
            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                options.StandardOutputCallback?.Invoke(line.TrimEnd('\r'));
            }
        }

        if (!string.IsNullOrEmpty(error))
        {
            foreach (var line in error.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                options.StandardErrorCallback?.Invoke(line.TrimEnd('\r'));
            }
        }

        // Parse the JSON output
        try
        {
            var jsonOutput = outputBuilder.ToString().Trim();
            if (string.IsNullOrEmpty(jsonOutput))
            {
                return (exitCode, new CertificateTrustResult
                {
                    HasCertificates = false,
                    TrustLevel = null,
                    Certificates = []
                });
            }

            var certificates = JsonSerializer.Deserialize(jsonOutput, JsonSourceGenerationContext.Default.ListDevCertInfo);
            if (certificates is null || certificates.Count == 0)
            {
                return (exitCode, new CertificateTrustResult
                {
                    HasCertificates = false,
                    TrustLevel = null,
                    Certificates = []
                });
            }

            // Find the highest versioned valid certificate
            var now = DateTimeOffset.Now;
            var validCertificates = certificates
                .Where(c => c.IsHttpsDevelopmentCertificate && c.ValidityNotBefore <= now && now <= c.ValidityNotAfter)
                .OrderByDescending(c => c.Version)
                .ToList();

            var highestVersionedCert = validCertificates.FirstOrDefault();
            var trustLevel = highestVersionedCert?.TrustLevel;

            return (exitCode, new CertificateTrustResult
            {
                HasCertificates = validCertificates.Count > 0,
                TrustLevel = trustLevel,
                Certificates = certificates
            });
        }
        catch (JsonException ex)
        {
            logger.LogDebug(ex, "Failed to parse dev-certs machine-readable output");
            return (exitCode, null);
        }
    }

    public async Task<int> TrustHttpCertificateAsync(
        DotNetCliRunnerInvocationOptions options,
        CancellationToken cancellationToken)
    {
        var managedPath = GetManagedPath();

        var arguments = new[] { "dev-certs", "--trust" };

        logger.LogDebug("Running: {ManagedPath} {Args}", managedPath, string.Join(" ", arguments));

        var (exitCode, output, error) = await LayoutProcessRunner.RunAsync(
            managedPath,
            arguments,
            ct: cancellationToken);

        // Forward output/error to callers
        if (!string.IsNullOrEmpty(output))
        {
            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                options.StandardOutputCallback?.Invoke(line.TrimEnd('\r'));
            }
        }

        if (!string.IsNullOrEmpty(error))
        {
            foreach (var line in error.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                options.StandardErrorCallback?.Invoke(line.TrimEnd('\r'));
            }
        }

        return exitCode;
    }

    private string GetManagedPath()
    {
        var layout = layoutDiscovery.DiscoverLayout();
        if (layout is null)
        {
            throw new InvalidOperationException("Bundle layout not found. Cannot run dev-certs in bundle mode.");
        }

        var managedPath = layout.GetManagedPath();
        if (managedPath is null || !File.Exists(managedPath))
        {
            throw new InvalidOperationException("aspire-managed not found in layout.");
        }

        return managedPath;
    }
}
