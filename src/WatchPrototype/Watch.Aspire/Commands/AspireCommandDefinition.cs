// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

internal abstract class AspireCommandDefinition : Command
{
    public readonly Option<bool> QuietOption = new("--quiet") { Arity = ArgumentArity.Zero };
    public readonly Option<bool> VerboseOption = new("--verbose") { Arity = ArgumentArity.Zero };

    protected AspireCommandDefinition(string name, string description)
        : base(name, description)
    {
        Options.Add(VerboseOption);
        Options.Add(QuietOption);

        VerboseOption.Validators.Add(v =>
        {
            if (v.HasOption(QuietOption) && v.HasOption(VerboseOption))
            {
                v.AddError("Cannot specify both '--quiet' and '--verbose' options.");
            }
        });
    }

    public LogLevel GetLogLevel(ParseResult parseResult)
        => parseResult.GetValue(QuietOption) ? LogLevel.Warning : parseResult.GetValue(VerboseOption) ? LogLevel.Debug : LogLevel.Information;
}
