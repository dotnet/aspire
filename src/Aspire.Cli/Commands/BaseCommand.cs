// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Configuration;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Commands;

internal abstract class BaseCommand : Command
{
    protected virtual bool UpdateNotificationsEnabled { get; } = true;

    protected BaseCommand(string name, string description, IFeatures features, ICliUpdateNotifier updateNotifier) : base(name, description)
    {
        SetAction(async (parseResult, cancellationToken) =>
        {
            // TODO: SDK install goes here in the future.

            var exitCode = await ExecuteAsync(parseResult, cancellationToken);

            if (UpdateNotificationsEnabled && features.IsFeatureEnabled(KnownFeatures.UpdateNotificationsEnabled, true))
            {
                try
                {
                    updateNotifier.NotifyIfUpdateAvailable();
                }
                catch
                {
                    // Ignore any errors during update check to avoid impacting the main command
                }
            }

            return exitCode;
        });
    }

    protected abstract Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken);
}
