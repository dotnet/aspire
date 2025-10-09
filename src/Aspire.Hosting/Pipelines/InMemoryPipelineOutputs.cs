// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Pipelines;

internal sealed class InMemoryPipelineOutputs : IPipelineOutputs
{
    private readonly Dictionary<string, object> _outputs = new();

    public void Set<T>(string key, T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _outputs[key] = value;
    }

    public bool TryGet<T>(string key, [NotNullWhen(true)] out T? value)
    {
        if (_outputs.TryGetValue(key, out var obj) && obj is T typed)
        {
            value = typed;
            return true;
        }
        value = default;
        return false;
    }
}
