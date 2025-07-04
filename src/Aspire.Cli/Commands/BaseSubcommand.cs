// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Configuration;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Commands;

internal abstract class BaseSubcommand(string name, string description, IFeatures features, ICliUpdateNotifier updateNotifier) : BaseCommand(name, description, features, updateNotifier)
{
    /// <summary>
    /// Extension-compatible method to execute the subcommand. Prompts for input if necessary.
    /// </summary>
    public abstract Task<int> ExecuteAsync(CancellationToken cancellationToken);
}
