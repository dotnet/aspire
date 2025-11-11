// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model.Assistant.Prompts;

namespace Aspire.Dashboard.Model.Assistant;

public record BuildIceBreakersContext(List<InitialPrompt> Prompts);

public class AIContext : IDisposable
{
    private readonly IAIContextProvider _provider;
    private readonly Action _raiseChange;
    private bool _isDisposed;

    public required string Description { get; init; }
    public Action<IceBreakersBuilder, BuildIceBreakersContext>? BuildIceBreakers { get; set; }

    public AIContext(IAIContextProvider provider, Action raiseChange)
    {
        _provider = provider;
        _raiseChange = raiseChange;
    }

    public void ContextHasChanged()
    {
        if (!_isDisposed)
        {
            _raiseChange();
        }
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _provider?.Remove(this);
            _isDisposed = true;
        }
    }
}
