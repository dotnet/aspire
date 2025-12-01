// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Commands;

internal class TestCommand : BaseCommand
{
    private readonly AspireCliTelemetry _telemetry;

    public TestCommand(
        IInteractionService interactionService,
        AspireCliTelemetry telemetry,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext)
        : base("test", TestCommandStrings.Description, features, updateNotifier, executionContext, interactionService)
    {
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(features);

        _telemetry = telemetry;
    }

    protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        using var activity = _telemetry.ActivitySource.StartActivity(this.Name);

        // Placeholder implementation
        InteractionService.DisplayEmptyLine();
        InteractionService.DisplaySuccess("Test command executed successfully (placeholder)");
        
        return Task.FromResult(ExitCodeConstants.Success);
    }
}
