// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Certificates;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Commands;

/// <summary>
/// Subcommand that removes all HTTPS development certificates.
/// </summary>
internal sealed class CertificatesCleanCommand : BaseCommand
{
    private readonly ICertificateToolRunner _certificateToolRunner;

    public CertificatesCleanCommand(ICertificateToolRunner certificateToolRunner, IInteractionService interactionService, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext, AspireCliTelemetry telemetry)
        : base("clean", CertificatesCommandStrings.CleanDescription, features, updateNotifier, executionContext, interactionService, telemetry)
    {
        _certificateToolRunner = certificateToolRunner;
    }

    protected override bool UpdateNotificationsEnabled => false;

    protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        InteractionService.DisplayMessage(KnownEmojis.Information, CertificatesCommandStrings.CleanProgress);

        var success = _certificateToolRunner.CleanHttpCertificate();

        if (success)
        {
            InteractionService.DisplaySuccess(CertificatesCommandStrings.CleanSuccess);
            return Task.FromResult(ExitCodeConstants.Success);
        }

        InteractionService.DisplayError(CertificatesCommandStrings.CleanFailure);
        return Task.FromResult(ExitCodeConstants.FailedToTrustCertificates);
    }
}
