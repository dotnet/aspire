// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Help;
using System.Globalization;
using Aspire.Cli.Certificates;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Aspire.Cli.Utils.EnvironmentChecker;

namespace Aspire.Cli.Commands;

internal sealed class CertificatesCommand : BaseCommand
{
    internal override HelpGroup HelpGroup => HelpGroup.ToolsAndConfiguration;

    public CertificatesCommand(ICertificateToolRunner certificateToolRunner, IInteractionService interactionService, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext, AspireCliTelemetry telemetry)
        : base("certificates", CertificatesCommandStrings.Description, features, updateNotifier, executionContext, interactionService, telemetry)
    {
        var cleanCommand = new CleanCommand(certificateToolRunner, interactionService, features, updateNotifier, executionContext, telemetry);
        var trustCommand = new TrustCommand(certificateToolRunner, interactionService, features, updateNotifier, executionContext, telemetry);

        Subcommands.Add(cleanCommand);
        Subcommands.Add(trustCommand);
    }

    protected override bool UpdateNotificationsEnabled => false;

    protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        new HelpAction().Invoke(parseResult);
        return Task.FromResult(ExitCodeConstants.InvalidCommand);
    }

    private sealed class CleanCommand : BaseCommand
    {
        private readonly ICertificateToolRunner _certificateToolRunner;

        public CleanCommand(ICertificateToolRunner certificateToolRunner, IInteractionService interactionService, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext, AspireCliTelemetry telemetry)
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

    private sealed class TrustCommand : BaseCommand
    {
        private readonly ICertificateToolRunner _certificateToolRunner;

        public TrustCommand(ICertificateToolRunner certificateToolRunner, IInteractionService interactionService, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext, AspireCliTelemetry telemetry)
            : base("trust", CertificatesCommandStrings.TrustDescription, features, updateNotifier, executionContext, interactionService, telemetry)
        {
            _certificateToolRunner = certificateToolRunner;
        }

        protected override bool UpdateNotificationsEnabled => false;

        protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            InteractionService.DisplayMessage(KnownEmojis.Information, CertificatesCommandStrings.TrustProgress);

            var result = _certificateToolRunner.TrustHttpCertificate();

            if (DevCertsCheck.IsSuccessfulTrustResult(result))
            {
                InteractionService.DisplaySuccess(CertificatesCommandStrings.TrustSuccess);
                return Task.FromResult(ExitCodeConstants.Success);
            }

            InteractionService.DisplayError(CertificatesCommandStrings.TrustFailure);
            var details = string.Format(CultureInfo.CurrentCulture, CertificatesCommandStrings.TrustFailureDetailsFormat, result);
            InteractionService.DisplayError(details);
            return Task.FromResult(ExitCodeConstants.FailedToTrustCertificates);
        }
    }
}
