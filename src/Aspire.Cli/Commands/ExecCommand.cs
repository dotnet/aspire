// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;

namespace Aspire.Cli.Commands;

internal class ExecCommand : BaseCommand
{
    public ExecCommand(string name, string description) : base(name, description)
    {
    }

    protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
