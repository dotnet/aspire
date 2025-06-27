// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Configuration;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Commands;

internal abstract class BaseCommand : Command
{
    protected virtual bool UpdateNotificationsEnabled { get; } = true;

    protected BaseCommand(string name, string description, IFeatures features, ICliUpdateNotififier updateNotifier) : base(name, description)
    {
        SetAction(async (parseResult, cancellationToken) =>
        {
            // TODO: SDK install goes here in the future.

            var exitCode = await ExecuteAsync(parseResult, cancellationToken);

            if (UpdateNotificationsEnabled && features.IsFeatureEnabled(KnownFeatures.UpdateNotificationsEnabled, true))
            {
                try
                {
                    var currentDirectory = new DirectoryInfo(Environment.CurrentDirectory);

                    // We use a separate CTS here because we want this check to run even if we've got a cancellation,
                    // but we'll only wait so long before we get details back about updates
                    // being available (it should already be in the cache for longer running
                    // commands and some commands will opt out entirely)
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    await updateNotifier.NotifyIfUpdateAvailableAsync(currentDirectory, cancellationToken: cts.Token);
                }
                catch
                {
                    // Ignore any errors during update check to avoid impacting the main command
                }
            }
        });
    }

    protected abstract Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken);
}
