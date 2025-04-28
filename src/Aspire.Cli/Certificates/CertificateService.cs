// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Cli.Interaction;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Certificates;

internal interface ICertificateService
{
    Task EnsureCertificatesTrustedAsync(IDotNetCliRunner runner, CancellationToken cancellationToken);
}

internal sealed class CertificateService(IInteractionService interactionService) : ICertificateService
{
    private readonly ActivitySource _activitySource = new ActivitySource(nameof(CertificateService));

    public async Task EnsureCertificatesTrustedAsync(IDotNetCliRunner runner, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity(nameof(EnsureCertificatesTrustedAsync), ActivityKind.Client);

        var ensureCertificateCollector = new OutputCollector();
        var checkExitCode = await interactionService.ShowStatusAsync(
            ":locked_with_key: Checking certificates...",
            async () => {
                var options = new DotNetCliRunnerInvocationOptions
                {
                    StandardOutputCallback = ensureCertificateCollector.AppendOutput,
                    StandardErrorCallback = ensureCertificateCollector.AppendError,
                };
                var result = await runner.CheckHttpCertificateAsync(
                    options,
                    cancellationToken);
                return result;
            });

        if (checkExitCode != 0)
        {
            var options = new DotNetCliRunnerInvocationOptions
            {
                StandardOutputCallback = ensureCertificateCollector.AppendOutput,
                StandardErrorCallback = ensureCertificateCollector.AppendError,
            };
            var trustExitCode = await interactionService.ShowStatusAsync(
                ":locked_with_key: Trusting certificates...",
                () => runner.TrustHttpCertificateAsync(
                    options,
                    cancellationToken));

            if (trustExitCode != 0)
            {
                interactionService.DisplayLines(ensureCertificateCollector.GetLines());
                throw new CertificateServiceException($"Failed to trust certificates, trust command failed with exit code: {trustExitCode}");
            }
        }
    }
}

public sealed class CertificateServiceException(string message) : Exception(message)
{

}