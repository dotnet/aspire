// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;

namespace Aspire.Cli.Commands;

internal abstract class BaseCommand : Command
{
    protected BaseCommand(string name, string description) : base(name, description)
    {
        SetAction(ExecuteAsync);
    }

    protected abstract Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken);
}
