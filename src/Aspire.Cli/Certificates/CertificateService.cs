// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
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
            $":locked_with_key: {Strings.CheckingCertificates}",
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
                $":locked_with_key: {Strings.TrustingCertificates}",
                () => runner.TrustHttpCertificateAsync(
                    options,
                    cancellationToken));

            if (trustExitCode != 0)
            {
                interactionService.DisplayLines(ensureCertificateCollector.GetLines());
                interactionService.DisplayMessage("warning", string.Format(CultureInfo.CurrentCulture, Strings.CertificatesMayNotBeFullyTrusted, trustExitCode));
            }
        }
    }
}

public sealed class CertificateServiceException(string message) : Exception(message)
{

}
