// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;

namespace Aspire.Cli.Templating;

internal class CallbackTemplate(
    string name,
    string description,
    Func<string, string> pathDeriverCallback,
    Action<Command> applyOptionsCallback,
    Func<CallbackTemplate, TemplateInputs, ParseResult, CancellationToken, Task<TemplateResult>> applyTemplateCallback,
    TemplateRuntime runtime = TemplateRuntime.DotNet,
    Func<string, bool>? supportsLanguageCallback = null,
    IReadOnlyList<string>? selectableAppHostLanguages = null) : ITemplate
{
    public string Name => name;

    public string Description => description;

    public TemplateRuntime Runtime => runtime;

    public Func<string, string> PathDeriver => pathDeriverCallback;

    public bool SupportsLanguage(string languageId)
    {
        return supportsLanguageCallback?.Invoke(languageId) ?? true;
    }

    public IReadOnlyList<string> SelectableAppHostLanguages { get; } = selectableAppHostLanguages ?? [];

    public void ApplyOptions(Command command)
    {
        applyOptionsCallback?.Invoke(command);
    }

    public Task<TemplateResult> ApplyTemplateAsync(TemplateInputs inputs, ParseResult parseResult, CancellationToken cancellationToken)
    {
        return applyTemplateCallback(this, inputs, parseResult, cancellationToken);
    }
}
