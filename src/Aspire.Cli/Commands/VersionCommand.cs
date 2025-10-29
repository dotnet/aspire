// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Aspire.Shared;

namespace Aspire.Cli.Commands;

internal sealed class VersionCommand : BaseCommand
{
    public VersionCommand(IInteractionService interactionService, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext)
        : base("version", VersionCommandStrings.Description, features, updateNotifier, executionContext, interactionService)
    {
        ArgumentNullException.ThrowIfNull(interactionService);
    }

    protected override bool UpdateNotificationsEnabled => false;
    protected override bool AppendEmptyLineAfterExecution => false;

    protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var version = GetVersion();
        InteractionService.DisplayMessage("information", version);
        return Task.FromResult(ExitCodeConstants.Success);
    }

    /// <summary>
    /// Gets the current CLI version. This method is shared between the version command and --version option.
    /// </summary>
    internal static string GetVersion()
    {
        return PackageUpdateHelpers.GetCurrentAssemblyVersion() ?? "Unknown";
    }
}
