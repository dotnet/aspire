// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Aspire.Cli.DotNet;
using Aspire.Cli.Layout;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Certificates;

/// <summary>
/// Certificate tool runner that uses the bundled dev-certs DLL with the bundled runtime.
/// </summary>
internal sealed class BundleCertificateToolRunner(
    LayoutConfiguration layout,
    ILogger<BundleCertificateToolRunner> logger) : ICertificateToolRunner
{
    public async Task<(int ExitCode, CertificateTrustResult? Result)> CheckHttpCertificateMachineReadableAsync(
        DotNetCliRunnerInvocationOptions options,
        CancellationToken cancellationToken)
    {
        var muxerPath = layout.GetMuxerPath();
        var devCertsPath = layout.GetDevCertsPath();

        if (muxerPath is null)
        {
            throw new InvalidOperationException("Bundle runtime not found. The bundle may be corrupt.");
        }

        if (devCertsPath is null || !File.Exists(devCertsPath))
        {
            throw new InvalidOperationException("dev-certs tool not found in bundle. The bundle may be corrupt or incomplete.");
        }

        var outputBuilder = new StringBuilder();

        var startInfo = new ProcessStartInfo(muxerPath)
        {
            WorkingDirectory = Environment.CurrentDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        // Use ArgumentList to prevent command injection
        startInfo.ArgumentList.Add(devCertsPath);
        startInfo.ArgumentList.Add("https");
        startInfo.ArgumentList.Add("--check-trust-machine-readable");

        using var process = new Process { StartInfo = startInfo };

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data is not null)
            {
                outputBuilder.AppendLine(e.Data);
                options.StandardOutputCallback?.Invoke(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data is not null)
            {
                options.StandardErrorCallback?.Invoke(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        var exitCode = process.ExitCode;

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

            var certificates = JsonSerializer.Deserialize(jsonOutput, Aspire.Cli.JsonSourceGenerationContext.Default.ListDevCertInfo);
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
        var muxerPath = layout.GetMuxerPath();
        var devCertsPath = layout.GetDevCertsPath();

        if (muxerPath is null)
        {
            throw new InvalidOperationException("Bundle runtime not found. The bundle may be corrupt.");
        }

        if (devCertsPath is null || !File.Exists(devCertsPath))
        {
            throw new InvalidOperationException("dev-certs tool not found in bundle. The bundle may be corrupt or incomplete.");
        }

        var startInfo = new ProcessStartInfo(muxerPath)
        {
            WorkingDirectory = Environment.CurrentDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        // Use ArgumentList to prevent command injection
        startInfo.ArgumentList.Add(devCertsPath);
        startInfo.ArgumentList.Add("https");
        startInfo.ArgumentList.Add("--trust");

        using var process = new Process { StartInfo = startInfo };

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data is not null)
            {
                options.StandardOutputCallback?.Invoke(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data is not null)
            {
                options.StandardErrorCallback?.Invoke(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        return process.ExitCode;
    }
}