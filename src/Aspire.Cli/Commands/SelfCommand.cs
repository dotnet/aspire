// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;

namespace Aspire.Cli.Commands;

internal sealed class SelfCommand : Command
{
    public SelfCommand(SelfUpdateCommand updateCommand) : base("self", "Manage the Aspire CLI itself")
    {
        ArgumentNullException.ThrowIfNull(updateCommand);

        Subcommands.Add(updateCommand);
    }
}
