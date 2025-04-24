// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Cli.Interaction;

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

        var checkExitCode = await interactionService.ShowStatusAsync(
            ":locked_with_key: Checking certificates...",
            () => runner.CheckHttpCertificateAsync(
                new DotNetCliRunnerInvocationOptions(),
                cancellationToken));

        if (checkExitCode != 0)
        {
            var trustExitCode = await interactionService.ShowStatusAsync(
                ":locked_with_key: Trusting certificates...",
                () => runner.TrustHttpCertificateAsync(
                    new DotNetCliRunnerInvocationOptions(),
                    cancellationToken));

            if (trustExitCode != 0)
            {
                throw new InvalidOperationException($"Failed to trust certificates, trust command failed with exit code: {trustExitCode}");
            }
        }
    }
}