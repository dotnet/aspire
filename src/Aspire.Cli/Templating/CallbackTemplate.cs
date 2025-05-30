// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Commands;

namespace Aspire.Cli.Templating;

internal class CallbackTemplate(string name, string description, Func<string, string> pathDeriverCallback, Action<TemplateCommand> applyOptionsCallback, Func<CallbackTemplate, ParseResult, CancellationToken, Task<int>> applyTemplateCallback) : ITemplate
{
    public string Name => name;

    public string Description => description;

    public Func<string, string> PathDeriver => pathDeriverCallback;

    public void ApplyOptions(TemplateCommand command)
    {
        applyOptionsCallback?.Invoke(command);
    }

    public Task<int> ApplyTemplateAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        return applyTemplateCallback(this, parseResult, cancellationToken);
    }
}