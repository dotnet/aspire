// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Interactivity;
using System.Diagnostics;

namespace Aspire.Cli.Utils;

internal static class CertificatesHelper
{
    private static readonly ActivitySource s_activitySource = new ActivitySource(nameof(CertificatesHelper));

    internal static async Task EnsureCertificatesTrustedAsync(IInteractivityService interactivityService, IDotNetCliRunner runner, CancellationToken cancellationToken)
    {
        using var activity = s_activitySource.StartActivity(nameof(EnsureCertificatesTrustedAsync), ActivityKind.Client);

        var checkExitCode = await interactivityService.ShowStatusAsync(
            ":locked_with_key: Checking certificates...",
            async () => {
                return await runner.CheckHttpCertificateAsync(cancellationToken);
            });

        if (checkExitCode != 0)
        {
            var trustExitCode = await interactivityService.ShowStatusAsync(
                ":locked_with_key: Trusting certificates...",
                async () => {
                    return await runner.TrustHttpCertificateAsync(cancellationToken);
                });

            if (trustExitCode != 0)
            {
                throw new InvalidOperationException($"Failed to trust certificates, trust command failed with exit code: {trustExitCode}");
            }
        }
    }
}