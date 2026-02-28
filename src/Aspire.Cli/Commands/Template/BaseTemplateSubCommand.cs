// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Commands.Template;

internal abstract class BaseTemplateSubCommand(
    string name,
    string description,
    IFeatures features,
    ICliUpdateNotifier updateNotifier,
    CliExecutionContext executionContext,
    IInteractionService interactionService,
    AspireCliTelemetry telemetry)
    : BaseCommand(name, description, features, updateNotifier, executionContext, interactionService, telemetry)
{
    protected override bool UpdateNotificationsEnabled => false;
}
