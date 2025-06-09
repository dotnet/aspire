// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Commands;

namespace Aspire.Cli.Templating;

internal interface ITemplate
{
    string Name { get; }
    string Description { get; }
    Func<string, string> PathDeriver { get; }
    void ApplyOptions(TemplateCommand command);
    Task<int> ApplyTemplateAsync(ParseResult parseResult, CancellationToken cancellationToken);
}
